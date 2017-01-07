namespace CoreFtp.Components.DirectoryListing
{
    using System.Collections.ObjectModel;
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
            return await ListNodeTypeAsync();
        }

        public override async Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync()
        {
            return await ListNodeTypeAsync( FtpNodeType.File );
        }

        public override async Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync()
        {
            return await ListNodeTypeAsync( FtpNodeType.Directory );
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

                var result = await ftpClient.SocketStream.SendCommandAsync( FtpCommand.MLSD );
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
    }
}