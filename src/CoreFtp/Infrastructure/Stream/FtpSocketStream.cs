namespace CoreFtp.Infrastructure.Stream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Components.DnsResolution;
    using Enum;
    using Extensions;
    using Microsoft.Extensions.Logging;

    public class FtpSocketStream : Stream
    {
        private readonly FtpClientConfiguration Configuration;
        public ILogger Logger;
        private readonly IDnsResolver dnsResolver;
        private Socket Socket;
        private Stream BaseStream;
        private int SocketPollInterval { get; } = 15000;
        private DateTime LastActivity = DateTime.Now;
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim( 1, 1 );
        private readonly SemaphoreSlim receiveSemaphore = new SemaphoreSlim( 1, 1 );

        public FtpSocketStream( FtpClientConfiguration configuration, IDnsResolver dnsResolver )
        {
            Configuration = configuration;
            this.dnsResolver = dnsResolver;
        }

        public override bool CanRead => BaseStream != null && BaseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => BaseStream != null && BaseStream.CanWrite;
        public override long Length => BaseStream?.Length ?? 0;

        public override long Position
        {
            get { return BaseStream?.Position ?? 0; }
            set { throw new InvalidOperationException(); }
        }

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
            return BaseStream?.Read( buffer, offset, count ) ?? 0;
        }


        public override void Write( byte[] buffer, int offset, int count )
        {
            BaseStream?.Write( buffer, offset, count );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            await BaseStream.WriteAsync( buffer, offset, count, cancellationToken );
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new InvalidOperationException();
        }


        public override void Flush()
        {
            if ( !IsConnected )
                throw new InvalidOperationException( "The FtpSocketStream object is not connected." );

            BaseStream?.Flush();
        }

        public override void SetLength( long value )
        {
            throw new InvalidOperationException();
        }

        public async Task ConnectAsync( CancellationToken token = default( CancellationToken ) )
        {
            Logger?.LogDebug( "Connecting" );
            var ipEndpoint = await dnsResolver.ResolveAsync( Configuration.Host, Configuration.Port, Configuration.IpVersion, token );

            Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp )
            {
                ReceiveTimeout = Configuration.TimeoutSeconds * 1000
            };
            Socket.Connect( ipEndpoint );

            BaseStream = new NetworkStream( Socket );
            LastActivity = DateTime.Now;
        }

        private async Task WriteLineAsync( string buf )
        {
            var data = Encoding.GetBytes( $"{buf}\r\n" );
            await WriteAsync( data, 0, data.Length );
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

        public int SocketDataAvailable()
        {
            return Socket?.Available ?? 0;
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

//                if ( !IsConnected )
//                {
//                    if ( command == FtpCommand.QUIT.ToString() )
//                    {
//                        return new FtpResponse
//                        {
//                            ResponseMessage = "Connection already closed",
//                            FtpStatusCode = FtpStatusCode.CommandOK
//                        };
//                    }
//                }

                Logger?.LogDebug( command.StartsWith( FtpCommand.PASS.ToString() ) 
                    ? "[FtpClient] Sending command: PASS *****" 
                    : $"[FtpClient] Sending command: {command}" );


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