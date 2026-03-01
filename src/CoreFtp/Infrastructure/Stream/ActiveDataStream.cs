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

        public ActiveDataStream(TcpListener listener, FtpControlStream controlStream, ILogger logger)
        {
            this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            this.controlStream = controlStream ?? throw new ArgumentNullException(nameof(controlStream));
            this.logger = logger;
        }

        private async Task EnsureAcceptedAsync(CancellationToken cancellationToken = default)
        {
            if (accepted)
                return;

            logger?.LogDebug("[ActiveDataStream] Accepting inbound data connection from server");

            var timeoutTask = Task.Delay(controlStream.Configuration.TimeoutSeconds * 1000, cancellationToken);
            var acceptTask = listener.AcceptTcpClientAsync();

            var completedTask = await Task.WhenAny(acceptTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("Timeout waiting for Active mode data connection.");
            }

            acceptedClient = await acceptTask;

            // Validate source IP to prevent data connection hijacking
            var remoteEndpoint = acceptedClient.Client.RemoteEndPoint as System.Net.IPEndPoint;
            var controlEndpoint = controlStream.RemoteEndPoint;

            if (remoteEndpoint != null && controlEndpoint != null &&
                !remoteEndpoint.Address.Equals(controlEndpoint.Address))
            {
                acceptedClient.Dispose();
                throw new FtpException(
                    $"Rejected active data connection from unexpected source IP: {remoteEndpoint.Address}");
            }

            innerStream = await controlStream.WrapDataStreamAsync(acceptedClient);
            accepted = true;
            logger?.LogDebug("[ActiveDataStream] Data connection accepted");
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(
                "Synchronous operations are not supported on this stream. Use ReadAsync instead.");
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            await EnsureAcceptedAsync(cancellationToken);
            return await innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(
                "Synchronous operations are not supported on this stream. Use WriteAsync instead.");
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await EnsureAcceptedAsync(cancellationToken);
            await innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

#if !NETSTANDARD2_0 && !NET462
        public override int Read(Span<byte> buffer)
        {
            throw new NotSupportedException(
                "Synchronous operations are not supported on this stream. Use ReadAsync instead.");
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await EnsureAcceptedAsync(cancellationToken);
            return await innerStream.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new NotSupportedException(
                "Synchronous operations are not supported on this stream. Use WriteAsync instead.");
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await EnsureAcceptedAsync(cancellationToken);
            await innerStream.WriteAsync(buffer, cancellationToken);
        }
#endif

        public override void Flush()
        {
            innerStream?.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                innerStream?.Dispose();
                acceptedClient?.Dispose();
                listener?.Stop();
                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
