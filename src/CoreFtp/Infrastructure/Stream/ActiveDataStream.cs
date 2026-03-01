namespace CoreFtp.Infrastructure.Stream
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A wrapper stream for Active FTP mode that lazily accepts the server's 
    /// inbound data connection on the first read operation. This defers the 
    /// TcpListener.AcceptTcpClient call until after the data command (e.g. LIST)
    /// has been sent on the control channel.
    /// </summary>
    internal class ActiveDataStream : Stream
    {
        private readonly TcpListener listener;
        private readonly FtpControlStream controlStream;
        private readonly ILogger logger;
        private Stream innerStream;
        private TcpClient acceptedClient;
        private bool accepted;
        private bool disposed;

        public ActiveDataStream( TcpListener listener, FtpControlStream controlStream, ILogger logger )
        {
            this.listener = listener ?? throw new ArgumentNullException( nameof( listener ) );
            this.controlStream = controlStream ?? throw new ArgumentNullException( nameof( controlStream ) );
            this.logger = logger;
        }

        private async Task EnsureAcceptedAsync()
        {
            if ( accepted )
                return;

            logger?.LogDebug( "[ActiveDataStream] Accepting inbound data connection from server" );
            acceptedClient = await listener.AcceptTcpClientAsync();
            innerStream = await controlStream.WrapDataStreamAsync( acceptedClient );
            accepted = true;
            logger?.LogDebug( "[ActiveDataStream] Data connection accepted" );
        }

        private void EnsureAccepted()
        {
            if ( accepted )
                return;

            EnsureAcceptedAsync().GetAwaiter().GetResult();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => innerStream?.Length ?? 0;

        public override long Position
        {
            get => innerStream?.Position ?? 0;
            set => throw new NotSupportedException();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            EnsureAccepted();
            return innerStream.Read( buffer, offset, count );
        }

        public override async Task<int> ReadAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            await EnsureAcceptedAsync();
            return await innerStream.ReadAsync( buffer, offset, count, cancellationToken );
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            EnsureAccepted();
            innerStream.Write( buffer, offset, count );
        }

        public override async Task WriteAsync( byte[] buffer, int offset, int count, CancellationToken cancellationToken )
        {
            await EnsureAcceptedAsync();
            await innerStream.WriteAsync( buffer, offset, count, cancellationToken );
        }

        public override void Flush()
        {
            innerStream?.Flush();
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new NotSupportedException();
        }

        public override void SetLength( long value )
        {
            throw new NotSupportedException();
        }

        protected override void Dispose( bool disposing )
        {
            if ( !disposed && disposing )
            {
                innerStream?.Dispose();
                acceptedClient?.Dispose();
                listener?.Stop();
                disposed = true;
            }

            base.Dispose( disposing );
        }
    }
}
