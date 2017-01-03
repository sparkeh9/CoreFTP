namespace CoreFtp.Infrastructure.Stream
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class FtpReadFileStream : Stream
    {
        private readonly Stream encapsulatedStream;
        private readonly FtpClient client;
        public ILogger Logger { get; set; }

        public override bool CanRead => encapsulatedStream.CanRead;
        public override bool CanSeek => encapsulatedStream.CanSeek;
        public override bool CanWrite => encapsulatedStream.CanWrite;
        public override long Length => encapsulatedStream.Length;

        public override long Position
        {
            get { return encapsulatedStream.Position; }
            set { encapsulatedStream.Position = value; }
        }


        public FtpReadFileStream( Stream encapsulatedStream, FtpClient client, ILogger logger )
        {
            Logger = logger;
            Logger?.LogDebug( "[FtpReadFileStream] Constructing" );
            this.encapsulatedStream = encapsulatedStream;
            this.client = client;
        }

        protected override void Dispose( bool disposing )
        {
            Logger?.LogDebug( "[FtpReadFileStream] Disposing" );
            base.Dispose( disposing );
            encapsulatedStream.Dispose();

            if ( client.SocketStream.HasResponsePending() )
            {
                Task.WaitAny( client.SocketStream.GetResponseAsync(), Task.Delay( 5000 ) );
            }
        }

        public override async Task FlushAsync( CancellationToken cancellationToken )
        {
            Logger?.LogDebug( "[FtpReadFileStream] FlushAsync" );
            await encapsulatedStream.FlushAsync( cancellationToken );
        }

        public override async Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            Logger?.LogDebug( "[FtpReadFileStream] ReadAsync" );
            return await encapsulatedStream.ReadAsync( buffer, offset, count, cancellationToken );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            Logger?.LogDebug( "[FtpReadFileStream] WriteAsync" );
            await encapsulatedStream.WriteAsync( buffer, offset, count, cancellationToken );
        }

        public override void Flush()
        {
            Logger?.LogDebug( "[FtpReadFileStream] Flush" );
            encapsulatedStream.Flush();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            Logger?.LogDebug( "[FtpReadFileStream] Read" );
            return encapsulatedStream.Read( buffer, offset, count );
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            Logger?.LogDebug( "[FtpReadFileStream] Seek" );
            return encapsulatedStream.Seek( offset, origin );
        }

        public override void SetLength( long value )
        {
            Logger?.LogDebug( "[FtpReadFileStream] SetLength" );
            encapsulatedStream.SetLength( value );
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            Logger?.LogDebug( "[FtpReadFileStream] Write" );
            encapsulatedStream.Write( buffer, offset, count );
        }
    }
}