namespace CoreFtp.Infrastructure.Stream
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public class FtpWriteFileStream : Stream
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


        public FtpWriteFileStream( Stream encapsulatedStream, FtpClient client, ILogger logger )
        {
            Logger = logger;
            this.encapsulatedStream = encapsulatedStream;
            this.client = client;
        }

        protected override void Dispose( bool disposing )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] Disposing" );
            base.Dispose( disposing );
            encapsulatedStream.Dispose();

            Task.WaitAny( client.CloseFileWriteStreamAsync() );
        }


        public override async Task FlushAsync( CancellationToken cancellationToken )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] FlushAsync" );
            await encapsulatedStream.FlushAsync( cancellationToken );
        }

        public override async Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] ReadAsync" );
            return await encapsulatedStream.ReadAsync( buffer, offset, count, cancellationToken );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            Logger?.LogDebug( $"[FtpWriteFileStream] WriteAsync" );
            await encapsulatedStream.WriteAsync( buffer, offset, count, cancellationToken );
        }


        public override void Flush()
        {
            Logger?.LogDebug( "[FtpWriteFileStream] Flush" );
            encapsulatedStream.Flush();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] Read" );
            return encapsulatedStream.Read( buffer, offset, count );
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] Seek" );
            return encapsulatedStream.Seek( offset, origin );
        }

        public override void SetLength( long value )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] SetLength" );
            encapsulatedStream.SetLength( value );
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            Logger?.LogDebug( "[FtpWriteFileStream] Write" );
            encapsulatedStream.Write( buffer, offset, count );
        }
    }
}