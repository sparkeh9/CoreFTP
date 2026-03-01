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
    using Microsoft.Extensions.Logging;
    using Parser;

    internal class ListDirectoryProvider : DirectoryProviderBase
    {
        private readonly List<IListDirectoryParser> directoryParsers;

        public ListDirectoryProvider( FtpClient ftpClient, ILogger logger, FtpClientConfiguration configuration )
        {
            this.ftpClient = ftpClient;
            this.logger = logger;
            this.configuration = configuration;

            directoryParsers = new List<IListDirectoryParser>
            {
                new UnixDirectoryParser( logger ),
                new DosDirectoryParser( logger ),
            };
        }

        internal void ClearParsers()
        {
            directoryParsers.Clear();
        }

        internal void AddParser(IListDirectoryParser parser)
        {
            directoryParsers.Add(parser);
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
                return await ListNodesAsync();
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
                return await ListNodesAsync( FtpNodeType.File );
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
                return await ListNodesAsync( FtpNodeType.Directory );
            }
            finally
            {
                ftpClient.dataSocketSemaphore.Release();
            }
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListAllEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodesEnumerableAsync( null, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListFilesEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodesEnumerableAsync( FtpNodeType.File, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        public override async IAsyncEnumerable<FtpNodeInformation> ListDirectoriesEnumerableAsync( [EnumeratorCancellation] CancellationToken cancellationToken = default )
        {
            await foreach ( var node in ListNodesEnumerableAsync( FtpNodeType.Directory, cancellationToken ).ConfigureAwait( false ) )
                yield return node;
        }

        /// <summary>
        /// Lists all nodes (files and directories) in the current working directory
        /// </summary>
        /// <param name="ftpNodeType"></param>
        /// <returns></returns>
        private async Task<ReadOnlyCollection<FtpNodeInformation>> ListNodesAsync( FtpNodeType? ftpNodeType = null )
        {
            EnsureLoggedIn();
            logger?.LogDebug( $"[ListDirectoryProvider] Listing {ftpNodeType}" );

            try
            {
                stream = await ftpClient.ConnectDataStreamAsync();

                var result = await ftpClient.ControlStream.SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.LIST
                } );

                if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) )
                    throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

                var directoryListing = RetrieveDirectoryListing();

                var nodes = ParseLines( directoryListing.ToList().AsReadOnly() )
                    .Where( x => !ftpNodeType.HasValue || x.NodeType == ftpNodeType )
                    .ToList();

                return nodes.AsReadOnly();
            }
            finally
            {
                stream.Dispose();
            }
        }

        /// <summary>
        /// Streams nodes as they are parsed from the LIST response
        /// </summary>
        private async IAsyncEnumerable<FtpNodeInformation> ListNodesEnumerableAsync( FtpNodeType? ftpNodeType, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            EnsureLoggedIn();
            logger?.LogDebug( $"[ListDirectoryProvider] Streaming {ftpNodeType}" );

            await ftpClient.dataSocketSemaphore.WaitAsync( cancellationToken );
            try
            {
                stream = await ftpClient.ConnectDataStreamAsync();

                var result = await ftpClient.ControlStream.SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.LIST
                } );

                if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) )
                    throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

                IListDirectoryParser parser = null;
                bool parserResolved = false;

                foreach ( string line in RetrieveDirectoryListing() )
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if ( !parserResolved )
                    {
                        parser = directoryParsers.Count == 1
                            ? directoryParsers[ 0 ]
                            : directoryParsers.FirstOrDefault( x => x.Test( line ) );
                        parserResolved = true;
                    }

                    if ( parser == null )
                        yield break;

                    var parsed = parser.Parse( line );

                    if ( parsed != null && ( !ftpNodeType.HasValue || parsed.NodeType == ftpNodeType ) )
                        yield return parsed;
                }
            }
            finally
            {
                stream?.Dispose();
                ftpClient.dataSocketSemaphore.Release();
            }
        }

        private IEnumerable<FtpNodeInformation> ParseLines( IReadOnlyList<string> lines )
        {
            if ( !lines.Any() )
                yield break;

            var parser = directoryParsers.Count == 1 
                ? directoryParsers[ 0 ]
                : directoryParsers.FirstOrDefault( x => x.Test( lines[ 0 ] ) );

            if ( parser == null )
                yield break;

            foreach ( string line in lines )
            {
                var parsed = parser.Parse( line );

                if ( parsed != null )
                    yield return parsed;
            }
        }
    }
}