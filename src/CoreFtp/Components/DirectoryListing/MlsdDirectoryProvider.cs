namespace CoreFtp.Components.DirectoryListing
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Enum;
    using Infrastructure;
    using System.Linq;
    using Infrastructure.Extensions;
    using Microsoft.Extensions.Logging;

    internal class MlsdDirectoryProvider : DirectoryProviderBase
    {
        public MlsdDirectoryProvider( FtpClient ftpClient, ILogger logger, FtpClientConfiguration configuration )
        {
            this.ftpClient = ftpClient;
            this.configuration = configuration;
            this.logger = logger;
        }

        private void EnsureLoggedIn()
        {
            if ( !ftpClient.IsConnected || !ftpClient.IsAuthenticated )
                throw new FtpException( "User must be logged in" );
        }

        public override async Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync()
        {
            try
            {
                await ftpClient.dataSocketSemaphore.WaitAsync();
                return await ListNodeTypeAsync();
            }
            finally
            {
                ftpClient.dataSocketSemaphore.Release();
            }
        }

        public override async Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync()
        {
            try
            {
                await ftpClient.dataSocketSemaphore.WaitAsync();
                return await ListNodeTypeAsync( FtpNodeType.File );
            }
            finally
            {
                ftpClient.dataSocketSemaphore.Release();
            }
        }

        public override async Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync()
        {
            try
            {
                await ftpClient.dataSocketSemaphore.WaitAsync();
                return await ListNodeTypeAsync( FtpNodeType.Directory );
            }
            finally
            {
                ftpClient.dataSocketSemaphore.Release();
            }
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListAllEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodeTypeEnumerableAsync( null, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListFilesEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodeTypeEnumerableAsync( FtpNodeType.File, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListDirectoriesEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodeTypeEnumerableAsync( FtpNodeType.Directory, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        /// <summary>
        /// Lists all nodes (files and directories) in the current working directory
        /// </summary>
        /// <param name="ftpNodeType"></param>
        /// <returns></returns>
        private async Task<ReadOnlyCollection<FtpNodeInformation>> ListNodeTypeAsync( FtpNodeType? ftpNodeType = null )
        {
            string nodeTypeString = !ftpNodeType.HasValue
                ? "all"
                : ftpNodeType.Value == FtpNodeType.File
                    ? "file"
                    : "dir";

            logger?.LogDebug( $"[MlsdDirectoryProvider] Listing {ftpNodeType}" );

            EnsureLoggedIn();

            try
            {
                stream = await ftpClient.ConnectDataStreamAsync();
                if ( stream == null )
                    throw new FtpException( "Could not establish a data connection" );

                var result = await ftpClient.ControlStream.SendCommandAsync( FtpCommand.MLSD );
                if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) && ( result.FtpStatusCode != FtpStatusCode.ClosingData ) )
                    throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

                var directoryListing = RetrieveDirectoryListing().ToList();

                var nodes = ( from node in directoryListing
                              where !node.IsNullOrWhiteSpace()
                              where !ftpNodeType.HasValue || node.Contains( $"type={nodeTypeString}" )
                              select node.ToFtpNode() )
                    .ToList();


                return nodes.AsReadOnly();
            }
            finally
            {
                stream?.Dispose();
                stream = null;
            }
        }

        /// <summary>
        /// Streams nodes as they are parsed from the MLSD response
        /// </summary>
        private async IAsyncEnumerable<FtpNodeInformation> ListNodeTypeEnumerableAsync( FtpNodeType? ftpNodeType, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            string nodeTypeString = !ftpNodeType.HasValue
                ? "all"
                : ftpNodeType.Value == FtpNodeType.File
                    ? "file"
                    : "dir";

            logger?.LogDebug( $"[MlsdDirectoryProvider] Streaming {ftpNodeType}" );

            EnsureLoggedIn();

            await ftpClient.dataSocketSemaphore.WaitAsync( cancellationToken );
            try
            {
                stream = await ftpClient.ConnectDataStreamAsync();
                if ( stream == null )
                    throw new FtpException( "Could not establish a data connection" );

                var result = await ftpClient.ControlStream.SendCommandAsync( FtpCommand.MLSD );
                if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) && ( result.FtpStatusCode != FtpStatusCode.ClosingData ) )
                    throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

                foreach ( string line in RetrieveDirectoryListing() )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( line.IsNullOrWhiteSpace() )
                        continue;

                    if ( ftpNodeType.HasValue && !line.Contains( $"type={nodeTypeString}" ) )
                        continue;

                    yield return line.ToFtpNode();
                }
            }
            finally
            {
                stream?.Dispose();
                stream = null;
                ftpClient.dataSocketSemaphore.Release();
            }
        }
    }
}