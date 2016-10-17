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
    using Parser;

    internal class ListDirectoryProvider : IDirectoryProvider
    {
        private readonly FtpClient ftpClient;
        private readonly FtpClientConfiguration configuration;
        private readonly List<IListDirectoryParser> directoryParsers;

        public ListDirectoryProvider( FtpClient ftpClient, FtpClientConfiguration configuration )
        {
            this.ftpClient = ftpClient;
            this.configuration = configuration;

            directoryParsers = new List<IListDirectoryParser>
            {
                new UnixDirectoryParser()
            };
        }

        private void EnsureLoggedIn()
        {
            if ( !ftpClient.IsConnected || !ftpClient.IsAuthenticated )
                throw new FtpException( "User must be logged in" );
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
        private async Task<ReadOnlyCollection<FtpNodeInformation>> ListNodeTypeAsync( FtpNodeType ftpNodeType )
        {
            EnsureLoggedIn();

            ftpClient.dataSocket = await ftpClient.ConnectDataSocketAsync();

            if ( ftpClient.dataSocket == null )
                throw new FtpException( "Could not establish a data connection" );

            var result = await ftpClient.SendCommandAsync( new FtpCommandEnvelope
                                                           {
                                                               FtpCommand = FtpCommand.LIST
                                                           } );

            if ( ( result.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( result.FtpStatusCode != FtpStatusCode.OpeningData ) )
                throw new FtpException( "Could not retrieve directory listing " + result.ResponseMessage );

            var directoryListing = await RetrieveDirectoryListingAsync();

            var nodes = ParseLines( directoryListing )
                .Where( x => x.NodeType == ftpNodeType )
                .ToList();

            return nodes.AsReadOnly();
        }

        private IEnumerable<FtpNodeInformation> ParseLines( IReadOnlyList<string> lines )
        {
            if ( !lines.Any() )
                yield break;

            var parser = directoryParsers.FirstOrDefault( x => x.Test( lines[ 0 ] ) );

            if ( parser == null )
                yield break;

            foreach ( string line in lines )
            {
                yield return parser.Parse( line );
            }
        }

        private async Task<string[]> RetrieveDirectoryListingAsync()
        {
            var maxTime = DateTime.Now.AddSeconds( configuration.TimeoutSeconds );
            bool hasTimedOut;

            var rawResult = new StringBuilder();

            do
            {
                var buffer = new byte[Constants.BUFFER_SIZE];

                int byteCount = ftpClient.dataSocket.Receive( buffer, buffer.Length, 0 );
                if ( byteCount == 0 ) break;

                rawResult.Append( Encoding.ASCII.GetString( buffer, 0, byteCount ) );

                hasTimedOut = ( configuration.TimeoutSeconds == 0 ) || ( DateTime.Now < maxTime );
            } while ( hasTimedOut );

            var lines = rawResult.Replace( Constants.CARRIAGE_RETURN, string.Empty )
                                 .ToString()
                                 .Split( Constants.LINEFEED );

            ftpClient.dataSocket.Shutdown( SocketShutdown.Both );

            await ftpClient.GetResponseAsync();
            return lines;
        }
    }
}