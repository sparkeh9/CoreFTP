namespace CoreFtp.Components.DirectoryListing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Enum;
    using Infrastructure;
    using System.Linq;
    using Infrastructure.Extensions;
    using Microsoft.Extensions.Logging;

    internal class MlsdDirectoryProvider : IDirectoryProvider
    {
        private readonly FtpClient ftpClient;
        private readonly FtpClientConfiguration configuration;
        private readonly ILogger logger;
        private Socket socket;

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

        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync()
        {
            return await ListNodeTypeAsync();
        }

        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync()
        {
            return await ListNodeTypeAsync( FtpNodeType.File );
        }

        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync()
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

            socket = await ftpClient.ConnectDataSocketAsync();

            if ( socket == null )
                throw new FtpException( "Could not establish a data connection" );

            var result = await ftpClient.SendCommandAsync( FtpCommand.MLSD );
            if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) && ( result.FtpStatusCode != FtpStatusCode.ClosingData ) )
                throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

            var directoryListing = await RetrieveDirectoryListingAsync();

            var nodes = ( from node in directoryListing
                          where !node.IsNullOrWhiteSpace()
                          where !ftpNodeType.HasValue || node.Contains( $"type={nodeTypeString}" )
                          select node.ToFtpNode() )
                .ToList();


            return nodes.AsReadOnly();
        }

        private Task<string[]> RetrieveDirectoryListingAsync()
        {
            var maxTime = DateTime.Now.AddSeconds( configuration.TimeoutSeconds );
            bool hasTimedOut;

            var rawResult = new StringBuilder();

            do
            {
                var buffer = new byte[Constants.BUFFER_SIZE];

                int byteCount = socket.Receive( buffer, buffer.Length, 0 );
                if ( byteCount == 0 ) break;

                rawResult.Append( ftpClient.Encoding.GetString( buffer, 0, byteCount ) );

                hasTimedOut = ( configuration.TimeoutSeconds == 0 ) || ( DateTime.Now < maxTime );
            } while ( hasTimedOut );

            var lines = rawResult.Replace( Constants.CARRIAGE_RETURN, string.Empty )
                                 .ToString()
                                 .Split( Constants.LINEFEED );

            socket.Shutdown( SocketShutdown.Both );

            return Task.FromResult( lines );
        }
    }
}