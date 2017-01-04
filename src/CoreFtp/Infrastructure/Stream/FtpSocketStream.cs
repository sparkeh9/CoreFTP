namespace CoreFtp.Infrastructure.Stream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Components.DnsResolution;
    using Enum;
    using Extensions;
    using Microsoft.Extensions.Logging;
    using Ssl;

    public class FtpSocketStream : Stream
    {
        private readonly FtpClientConfiguration Configuration;
        public ILogger Logger;
        private readonly IDnsResolver dnsResolver;
        private Socket Socket;
        private Stream BaseStream;

        private Stream NetworkStream => SslStream ?? BaseStream;
        private SslStream SslStream { get; set; }
        private int SocketPollInterval { get; } = 15000;
        private DateTime LastActivity = DateTime.Now;
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim( 1, 1 );
        private readonly SemaphoreSlim receiveSemaphore = new SemaphoreSlim( 1, 1 );

        event FtpSocketStreamSslValidation m_sslvalidate;
//
//        /// <summary>
//        /// Event is fired when a SSL certificate needs to be validated
//        /// </summary>
//        public event FtpSocketStreamSslValidation ValidateCertificate
//        {
//            add { m_sslvalidate += value; }
//            remove { m_sslvalidate -= value; }
//        }

        public FtpSocketStream( FtpClientConfiguration configuration, IDnsResolver dnsResolver )
        {
            Configuration = configuration;
            this.dnsResolver = dnsResolver;


//            ValidateCertificate += ( control, e ) =>
//            {
//                e.Accept = Configuration.IgnoreCertificateErrors ||
//                           ( e.PolicyErrors == SslPolicyErrors.None );
//            };
        }

        public override bool CanRead => NetworkStream != null && NetworkStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => NetworkStream != null && NetworkStream.CanWrite;
        public override long Length => NetworkStream?.Length ?? 0;

        public override long Position
        {
            get { return NetworkStream?.Position ?? 0; }
            set { throw new InvalidOperationException(); }
        }

        public bool IsEncrypted => SslStream != null && SslStream.IsEncrypted;

        public bool IsConnected
        {
            get
            {
                try
                {
                    if ( Socket == null || !Socket.Connected || ( !CanRead || !CanWrite ) )
                    {
                        Disconnect();
                        return false;
                    }

                    if ( LastActivity.HasIntervalExpired( DateTime.Now, SocketPollInterval ) )
                    {
                        Logger?.LogDebug( "Polling connection" );
                        if ( Socket.Poll( 500000, SelectMode.SelectRead ) && Socket.Available == 0 )
                        {
                            Disconnect();
                            return false;
                        }
                    }
                }
                catch ( SocketException socketException )
                {
                    Disconnect();
                    Logger?.LogError( $"FtpSocketStream.IsConnected: Caught and discarded SocketException while testing for connectivity: {socketException}" );
                    return false;
                }
                catch ( IOException ioException )
                {
                    Disconnect();
                    Logger?.LogError( $"FtpSocketStream.IsConnected: Caught and discarded IOException while testing for connectivity: {ioException}" );
                    return false;
                }

                return true;
            }
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            return NetworkStream?.Read( buffer, offset, count ) ?? 0;
        }


        public override void Write( byte[] buffer, int offset, int count )
        {
            NetworkStream?.Write( buffer, offset, count );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            await NetworkStream.WriteAsync( buffer, offset, count, cancellationToken );
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new InvalidOperationException();
        }


        public override void Flush()
        {
            if ( !IsConnected )
                throw new InvalidOperationException( "The FtpSocketStream object is not connected." );

            NetworkStream?.Flush();
        }

        public override void SetLength( long value )
        {
            throw new InvalidOperationException();
        }

        public async Task ConnectAsync( CancellationToken token = default( CancellationToken ) )
        {
            await ConnectSocketAsync( token );

            if ( SocketDataAvailable() )
            {
                // Throw away welcome message
                await GetResponseAsync( CancellationToken.None );
            }

            if ( !Configuration.ShouldEncrypt )
                return;

            if ( Configuration.EncryptionType == FtpEncryption.Implicit )
                await EncryptImplicitly( token );

            if ( Configuration.EncryptionType == FtpEncryption.Explicit )
                await EncryptExplicitly( token );
        }


        private async Task WriteLineAsync( string buf )
        {
            var data = Encoding.GetBytes( $"{buf}\r\n" );
            await WriteAsync( data, 0, data.Length, CancellationToken.None );
        }

        private string ReadLine( Encoding encoding )
        {
            if ( encoding == null )
                throw new ArgumentNullException( nameof( encoding ) );

            var data = new List<byte>();
            var buf = new byte[1];
            string line = null;

            while ( Read( buf, 0, buf.Length ) > 0 )
            {
                data.Add( buf[ 0 ] );
                if ( (char) buf[ 0 ] != '\n' )
                    continue;
                line = encoding.GetString( data.ToArray() ).Trim( '\r', '\n' );
                break;
            }

            return line;
        }

        private IEnumerable<string> ReadLines()
        {
            string line;
            while ( ( line = ReadLine( Encoding ) ) != null )
            {
                yield return line;
            }
        }

        public bool HasResponsePending()
        {
            return Socket != null && Socket.Available > 0;
        }

        public bool SocketDataAvailable()
        {
            return ( Socket?.Available ?? 0 ) > 0;
        }

        public async Task<FtpResponse> SendCommandAsync( FtpCommand command, CancellationToken token = default( CancellationToken ) )
        {
            return await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = command
            }, token );
        }

        public async Task<FtpResponse> SendCommandAsync( FtpCommandEnvelope envelope, CancellationToken token = default( CancellationToken ) )
        {
            string commandString = envelope.GetCommandString();
            return await SendCommandAsync( commandString, token );
        }

        public async Task<FtpResponse> SendCommandAsync( string command, CancellationToken token = default( CancellationToken ) )
        {
            await semaphore.WaitAsync( token );

            try
            {
                if ( HasResponsePending() )
                {
                    var staleDataResult = await GetResponseAsync( token );
                    Logger?.LogWarning( $"Stale data on socket {staleDataResult.ResponseMessage}" );
                }

                string commandToPrint = command.StartsWith( FtpCommand.PASS.ToString() )
                    ? "PASS *****"
                    : command;

                Logger?.LogDebug( $"[FtpClient] Sending command: {commandToPrint}" );
                await WriteLineAsync( command );

                var response = await GetResponseAsync( token );
                return response;
            }
            finally
            {
                semaphore.Release();
            }
        }


        public async Task<FtpResponse> GetResponseAsync( CancellationToken token = default( CancellationToken ) )
        {
            Logger?.LogDebug( "Getting Response" );

            if ( Encoding == null )
                throw new ArgumentNullException( nameof( Encoding ) );

            await receiveSemaphore.WaitAsync( token );

            try
            {
                var response = new FtpResponse();
                var data = new List<string>();

                foreach ( string line in ReadLines() )
                {
                    Logger?.LogDebug( line );
                    data.Add( line );

                    Match m;

                    if ( !( m = Regex.Match( line, "^(?<statusCode>[0-9]{3}) (?<message>.*)$" ) ).Success )
                        continue;

                    response.FtpStatusCode = m.Groups[ "statusCode" ].Value.ToStatusCode();
                    response.ResponseMessage = m.Groups[ "message" ].Value;
                    break;
                }
                response.Data = data.ToArray();
                return response;
            }
            finally
            {
                receiveSemaphore.Release();
            }
        }

        private async Task ConnectSocketAsync( CancellationToken token )
        {
            Logger?.LogDebug( "Connecting" );
            var ipEndpoint = await dnsResolver.ResolveAsync( Configuration.Host, Configuration.Port, Configuration.IpVersion, token );

            Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp )
            {
                ReceiveTimeout = Configuration.TimeoutSeconds * 1000
            };
            Socket.Connect( ipEndpoint );
            Socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true );

            BaseStream = new NetworkStream( Socket );
            LastActivity = DateTime.Now;
        }

        private async Task EncryptImplicitly( CancellationToken token )
        {
            await ActivateEncryptionAsync();

            var response = await GetResponseAsync( token );
            if ( !response.IsSuccess )
            {
                throw new IOException( $"Could not securely connect to host {Configuration.Host}:{Configuration.Port}" );
            }
        }

        private async Task EncryptExplicitly( CancellationToken token )
        {
            var response = await SendCommandAsync( "AUTH TLS", token );

            if ( !response.IsSuccess )
                throw new InvalidOperationException();

            await ActivateEncryptionAsync();
        }

        private async Task ActivateEncryptionAsync()
        {
            if ( !IsConnected )
                throw new InvalidOperationException( "The FtpSocketStream object is not connected." );

            if ( BaseStream == null )
                throw new InvalidOperationException( "The base network stream is null." );

            if ( SslStream != null )
                throw new InvalidOperationException( "SSL Encryption has already been enabled on this stream." );

            try
            {
                SslStream = new SslStream( BaseStream, true, ( sender, certificate, chain, sslPolicyErrors ) => OnValidateCertificate( certificate, chain, sslPolicyErrors ) );
                await SslStream.AuthenticateAsClientAsync( Configuration.Host, Configuration.ClientCertificates, Configuration.SslProtocols, true );
            }
            catch ( AuthenticationException e )
            {
                Logger?.LogError( $"Could not activate encryption for the connection: {e.Message}" );
                throw;
            }
        }

        private bool OnValidateCertificate( X509Certificate certificate, X509Chain chain, SslPolicyErrors errors )
        {
            if ( Configuration.IgnoreCertificateErrors )
                return true;

            return errors == SslPolicyErrors.None;
        }

        public void Disconnect()
        {
            try
            {
                Socket?.Shutdown( SocketShutdown.Both );
                BaseStream?.Dispose();
            }
            catch ( Exception exception )
            {
                Logger?.LogError( $"Exception caught: {exception}" );
            }
            finally
            {
                Socket = null;
                BaseStream = null;
            }
        }
    }
}