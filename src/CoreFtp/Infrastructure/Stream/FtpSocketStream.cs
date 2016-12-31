namespace CoreFtp.Infrastructure.Stream
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Security;
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
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim( 0, 1 );
        private readonly FtpClient client;
        private readonly IDnsResolver dnsResolver;
        private readonly FtpClientConfiguration configuration;

        /// <summary>
        /// Gets a value indicating if encryption is being used
        /// </summary>
        public bool IsEncrypted => SslStream != null;

        /// <summary>
        /// The non-encrypted stream
        /// </summary>
        private NetworkStream NetworkStream { get; set; } = null;

        /// <summary>
        /// The encrypted stream
        /// </summary>
        private SslStream SslStream { get; set; } = null;

        private Stream BaseStream
        {
            get
            {
                if ( SslStream != null )
                    return SslStream;

                return NetworkStream;
            }
        }

        private ILogger Logger { get; set; }
        private DateTime lastActivity = DateTime.Now;
        private Socket Socket { get; set; } = null;
        private int SocketPollInterval { get; set; } = 15000;
        internal int SocketDataAvailable => Socket?.Available ?? 0;


        /// <summary>
        /// Gets a value indicating if this stream can be read
        /// </summary>
        public override bool CanRead => NetworkStream != null && NetworkStream.CanRead;

        /// <summary>
        /// Gets a value indicating if this stream if seekable
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating if this stream can be written to
        /// </summary>
        public override bool CanWrite => NetworkStream != null && NetworkStream.CanWrite;

        public override long Position
        {
            get { return BaseStream?.Position ?? 0; }
            set { throw new InvalidOperationException(); }
        }

        /// <summary>
        /// Gets the length of the stream
        /// </summary>
        public override long Length => 0;

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

                    if ( lastActivity.HasIntervalExpired( DateTime.Now, SocketPollInterval ) )
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

        public override async Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            return BaseStream != null
                ? await BaseStream.ReadAsync( buffer, offset, count, cancellationToken )
                : 0;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            BaseStream?.Write( buffer, offset, count );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            if ( BaseStream != null )
                await BaseStream.WriteAsync( buffer, offset, count, cancellationToken );
        }

        internal int RawSocketRead( byte[] buffer )
        {
            int read = 0;

            if ( Socket != null && Socket.Connected )
            {
                read = Socket.Receive( buffer, buffer.Length, 0 );
            }

            return read;
        }

        public override void Flush()
        {
            if ( !IsConnected )
                throw new InvalidOperationException( "The FtpSocketStream object is not connected." );

            if ( BaseStream == null )
                throw new InvalidOperationException( "The base stream of the FtpSocketStream object is null." );

            BaseStream.Flush();
        }

        private void Disconnect()
        {
            try
            {
                Socket?.Shutdown( SocketShutdown.Both );
                NetworkStream?.Dispose();
                SslStream?.Dispose();
            }
            catch ( Exception exception )
            {
                Logger?.LogError( $"Exception caught: {exception}" );
            }
            finally
            {
                Socket = null;
                NetworkStream = null;
                SslStream = null;
            }
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Throws an InvalidOperationException
        /// </summary>
        /// <param name="value">Ignored</param>
        public override void SetLength( long value )
        {
            throw new InvalidOperationException();
        }

        public void WriteLine( Encoding encoding, string buf )
        {
            var data = encoding.GetBytes( string.Format( "{0}\r\n", buf ) );
            Write( data, 0, data.Length );
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

        private IEnumerable<string> ReadLines( Encoding encoding )
        {
            string line;
            while ( ( line = ReadLine( encoding ) ) != null )
            {
                yield return line;
            }
        }


        public async Task<FtpResponse> GetResponseAsync( Encoding encoding, CancellationToken token )
        {
            Logger?.LogDebug( "Getting Response" );

            if ( encoding == null )
                throw new ArgumentNullException( nameof( encoding ) );

            await semaphore.WaitAsync( token );

            var response = new FtpResponse();
            var data = new List<string>();

            foreach ( string line in ReadLines( encoding ) )
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

            semaphore.Release();
            response.Data = data.ToArray();
            return response;
        }

        public async Task<FtpResponse> GetResponseAsyncOld( Encoding encoding )
        {
            if ( encoding == null )
                throw new ArgumentNullException( nameof( encoding ) );

            await Task.Delay( 0 );
            Logger?.LogDebug( "[FtpClient] Getting Response" );

            do
            {
                var buffer = new byte[Constants.BUFFER_SIZE];
                string statusMessage = string.Empty;

                while ( true )
                {
                    int byteCount = Socket.Receive( buffer, buffer.Length, SocketFlags.None );
                    if ( byteCount == 0 )
                        break;

                    statusMessage += encoding.GetString( buffer, 0, byteCount );

                    if ( byteCount < buffer.Length )
                        break;
                }

                var message = statusMessage.Split( Constants.LINEFEED );

                statusMessage = statusMessage.Length > 2
                    ? message[ message.Length - 2 ]
                    : message[ 0 ];

                if ( !statusMessage.Substring( 3, 1 ).Equals( " " ) )
                    continue;

                Logger?.LogDebug( $"[FtpClient] {statusMessage}" );

                return new FtpResponse
                {
                    ResponseMessage = statusMessage,
                    FtpStatusCode = statusMessage.ToStatusCode(),
                    Data = message
                };
            } while ( true );
        }

        public void SetSocketOption( SocketOptionLevel level, SocketOptionName name, bool value )
        {
            if ( Socket == null )
                throw new InvalidOperationException( "The underlying socket is null. Have you established a connection?" );
            Socket.SetSocketOption( level, name, value );
        }


        public async Task ConnectAsync()
        {
            var ipEndpoint = await dnsResolver.ResolveAsync( configuration.Host, configuration.Port, configuration.IpVersion );

            Socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp )
            {
                ReceiveTimeout = configuration.TimeoutSeconds * 1000
            };
            Socket.Connect( ipEndpoint );
        }
    }
}