namespace CoreFtp
{
    using System.Threading.Tasks;
    using System.IO;
    using System.Threading;

    public class FtpWriteFileStream : Stream
    {
        private readonly Stream encapsulatedStream;
        private readonly FtpClient client;

        public override bool CanRead => encapsulatedStream.CanRead;
        public override bool CanSeek => encapsulatedStream.CanSeek;
        public override bool CanWrite => encapsulatedStream.CanWrite;
        public override long Length => encapsulatedStream.Length;

        public override long Position
        {
            get { return encapsulatedStream.Position; }
            set { encapsulatedStream.Position = value; }
        }


        public FtpWriteFileStream( Stream encapsulatedStream, FtpClient client )
        {
            this.encapsulatedStream = encapsulatedStream;
            this.client = client;
        }

        protected override void Dispose( bool disposing )
        {
            base.Dispose( disposing );
            encapsulatedStream.Dispose();
            client.CloseFileWriteStreamAsync().GetAwaiter().GetResult();
        }

        public override async Task FlushAsync( CancellationToken cancellationToken )
        {
            await encapsulatedStream.FlushAsync( cancellationToken );
        }

        public override async Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            return await encapsulatedStream.ReadAsync( buffer, offset, count, cancellationToken );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            await encapsulatedStream.WriteAsync( buffer, offset, count, cancellationToken );
        }


        public override void Flush()
        {
            encapsulatedStream.Flush();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            return encapsulatedStream.Read( buffer, offset, count );
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            return encapsulatedStream.Seek( offset, origin );
        }

        public override void SetLength( long value )
        {
            encapsulatedStream.SetLength( value );
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            encapsulatedStream.Write( buffer, offset, count );
        }
    }
}