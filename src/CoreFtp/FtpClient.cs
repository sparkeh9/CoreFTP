namespace CoreFtp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Components.DirectoryListing;
    using Components.DnsResolution;
    using Enum;
    using Infrastructure;
    using Infrastructure.Extensions;
    using Microsoft.Extensions.Logging;

    public class FtpClient : IDisposable
    {
        private readonly SemaphoreSlim commandSemaphore = new SemaphoreSlim( 1, 2 );

        private readonly IDnsResolver dnsResolver;
        private IDirectoryProvider directoryProvider;
        public FtpClientConfiguration Configuration { get; }
        public ILogger Logger { get; set; }
        internal IEnumerable<string> Features { get; set; }
        internal Socket commandSocket { get; set; }
        internal Socket dataSocket { get; set; }
        public bool IsConnected => ( commandSocket != null ) && commandSocket.Connected;
        public bool IsAuthenticated { get; set; }
        public string WorkingDirectory { get; set; } = "/";
        public Encoding Encoding { get; set; } = Encoding.ASCII;

        public FtpClient( FtpClientConfiguration configuration )
        {
            this.Configuration = configuration;

            if ( configuration.Host == null )
                throw new ArgumentNullException( nameof( configuration.Host ) );

            dnsResolver = new DnsResolver();
        }

        /// <summary>
        ///     Attempts to log the user in to the FTP Server
        /// </summary>
        /// <returns></returns>
        public async Task LoginAsync()
        {
            if ( IsConnected )
                await LogOutAsync();

            Configuration.BaseDirectory = $"/{Configuration.BaseDirectory.TrimStart( '/' )}";

            string username = Configuration.Username.IsNullOrWhiteSpace() ? Constants.ANONYMOUS_USER : Configuration.Username;

            Logger?.LogDebug( $"[FtpClient] Logging In {username}" );

            await ConnectCommandSocketAsync();
            var usrResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.USER,
                Data = username
            } );

            await BailIfResponseNotAsync( usrResponse, FtpStatusCode.SendPasswordCommand, FtpStatusCode.LoggedInProceed );

            var passResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.PASS,
                Data = username != Constants.ANONYMOUS_USER ? Configuration.Password : string.Empty
            } );

            await BailIfResponseNotAsync( passResponse, FtpStatusCode.LoggedInProceed );
            IsAuthenticated = true;

            Features = await DetermineFeaturesAsync();
            if (Encoding == Encoding.ASCII && Features.Any(x => x == Constants.UTF8))
            {
                Encoding = Encoding.UTF8;
            }

            if (Encoding == Encoding.UTF8)
            {
                // If the server supports UTF8 it should already be enabled and this
                // command should not matter however there are conflicting drafts
                // about this so we'll just execute it to be safe. 
                await SendCommandAsync("OPTS UTF8 ON");
            }

            await SetTransferMode( Configuration.Mode, Configuration.ModeSecondType );

            if ( Configuration.BaseDirectory != "/" )
                await CreateDirectoryAsync( Configuration.BaseDirectory );

            await ChangeWorkingDirectoryAsync( Configuration.BaseDirectory );

            directoryProvider = DetermineDirectoryProvider();
        }


        /// <summary>
        ///     Attemps to log the user out asynchronously, sends the QUIT command and terminates the command socket.
        /// </summary>
        public async Task LogOutAsync()
        {
            Logger?.LogDebug( "[FtpClient] Logging out" );
            if ( !IsConnected )
                return;

            await SendCommandAsync( FtpCommand.QUIT );
            commandSocket.Shutdown( SocketShutdown.Both );
            IsAuthenticated = false;
        }

        /// <summary>
        /// Changes the working directory to the given value for the current session
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task ChangeWorkingDirectoryAsync( string directory )
        {
            Logger?.LogDebug( $"[FtpClient] changing directory to {directory}" );
            if ( directory.IsNullOrWhiteSpace() || directory.Equals( "." ) )
                throw new ArgumentOutOfRangeException( nameof( directory ), "Directory supplied was incorrect" );

            EnsureLoggedIn();

            var response = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.CWD,
                Data = directory
            } );

            if ( response.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( response.ResponseMessage );

            var pwdResponse = await SendCommandAsync( FtpCommand.PWD );

            if ( pwdResponse.FtpStatusCode != FtpStatusCode.PathnameCreated )
                throw new FtpException( response.ResponseMessage );

            WorkingDirectory = pwdResponse.ResponseMessage.Split( '"' )[ 1 ];
        }

        /// <summary>
        /// Creates a directory on the FTP Server
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task CreateDirectoryAsync( string directory )
        {
            if ( directory.IsNullOrWhiteSpace() || directory.Equals( "." ) )
                throw new ArgumentOutOfRangeException( nameof( directory ), "Directory supplied was not valid" );

            Logger?.LogDebug( $"[FtpClient] Creating directory {directory}" );

            EnsureLoggedIn();

            await CreateDirectoryStructureRecursively( directory.Split( '/' ), directory.StartsWith( "/" ) );
        }

        /// <summary>
        /// Renames a file on the FTP server
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task RenameAsync( string from, string to )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Renaming from {from}, to {to}" );
            var renameFromResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.RNFR,
                Data = from
            } );

            if ( renameFromResponse.FtpStatusCode != FtpStatusCode.FileCommandPending )
                throw new FtpException( renameFromResponse.ResponseMessage );

            var renameToResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.RNTO,
                Data = to
            } );

            if ( renameToResponse.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( renameFromResponse.ResponseMessage );
        }

        /// <summary>
        /// Deletes the given directory from the FTP server
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task DeleteDirectoryAsync( string directory )
        {
            if ( directory.IsNullOrWhiteSpace() || directory.Equals( "." ) )
                throw new ArgumentOutOfRangeException( nameof( directory ), "Directory supplied was not valid" );

            if ( directory == "/" )
                return;

            Logger?.LogDebug( $"[FtpClient] Deleting directory {directory}" );

            EnsureLoggedIn();

            var rmdResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.RMD,
                Data = directory
            } );

            switch ( rmdResponse.FtpStatusCode )
            {
                case FtpStatusCode.CommandOK:
                case FtpStatusCode.FileActionOK:
                    return;

                case FtpStatusCode.ActionNotTakenFileUnavailable:
                    await DeleteNonEmptyDirectory( directory );
                    return;

                default:
                    throw new FtpException( rmdResponse.ResponseMessage );
            }
        }

        /// <summary>
        /// Deletes the given directory from the FTP server
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private async Task DeleteNonEmptyDirectory( string directory )
        {
            await ChangeWorkingDirectoryAsync( directory );

            var allNodes = await ListAllAsync();

            foreach ( var file in allNodes.Where( x => x.NodeType == FtpNodeType.File ) )
            {
                await DeleteFileAsync( file.Name );
            }

            foreach ( var dir in allNodes.Where( x => x.NodeType == FtpNodeType.Directory ) )
            {
                await DeleteDirectoryAsync( dir.Name );
            }

            await ChangeWorkingDirectoryAsync( ".." );
            await DeleteDirectoryAsync( directory );
        }

        /// <summary>
        /// Informs the FTP server of the client being used
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public async Task<FtpResponse> SetClientName( string clientName )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Setting client name to {clientName}" );

            return await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.CLNT,
                Data = clientName
            } );
        }

        /// <summary>
        /// Provides a stream which contains the data of the given filename on the FTP server
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<Stream> OpenFileReadStreamAsync( string fileName )
        {
            Logger?.LogDebug( $"[FtpClient] Opening file read stream for {fileName}" );

            var encapsulatedStream = await OpenFileStreamAsync( fileName, FtpCommand.RETR );
            return new FtpReadFileStream( encapsulatedStream, this, Logger );
        }

        /// <summary>
        /// Provides a stream which can be written to
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<Stream> OpenFileWriteStreamAsync( string fileName )
        {
            string filePath = WorkingDirectory.CombineAsUriWith( fileName );
            Logger?.LogDebug( $"[FtpClient] Opening file read stream for {filePath}" );
            var segments = filePath.Split( '/' ).Where( x => !x.IsNullOrWhiteSpace() ).ToList();
            await CreateDirectoryStructureRecursively( segments.Take( segments.Count - 1 ).ToArray(), filePath.StartsWith( "/" ) );
            return new FtpWriteFileStream( await OpenFileStreamAsync( filePath, FtpCommand.STOR ), this, Logger );
        }

        /// <summary>
        /// Closes the write stream and associated socket (if open), 
        /// </summary>
        /// <returns></returns>
        public async Task CloseFileWriteStreamAsync()
        {
            if ( !dataSocket.Connected )
                return;

            Logger?.LogDebug( "[FtpClient] Closing write file stream" );

            dataSocket.Shutdown( SocketShutdown.Both );

            await GetResponseAsync();
        }

        /// <summary>
        /// Lists all files in the current working directory
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync()
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Listing files in {WorkingDirectory}" );
            return await directoryProvider.ListAllAsync();
        }

        /// <summary>
        /// Lists all files in the current working directory
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync()
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Listing files in {WorkingDirectory}" );
            return await directoryProvider.ListFilesAsync();
        }

        /// <summary>
        /// Lists all directories in the current working directory
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync()
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Listing directories in {WorkingDirectory}" );
            return await directoryProvider.ListDirectoriesAsync();
        }


        /// <summary>
        /// Lists all directories in the current working directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task DeleteFileAsync( string fileName )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Deleting file {fileName}" );
            var deleResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.DELE,
                Data = fileName
            } );

            if ( deleResponse.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( deleResponse.ResponseMessage );
        }

        /// <summary>
        /// Determines the file size of the given file
        /// </summary>
        /// <param name="transferMode"></param>
        /// <param name="secondType"></param>
        /// <returns></returns>
        public async Task SetTransferMode( FtpTransferMode transferMode, char secondType = '\0' )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Setting transfer mode {transferMode}, {secondType}" );
            var typeResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.TYPE,
                Data = secondType != '\0'
                    ? $"{(char) transferMode} {secondType}"
                    : $"{(char) transferMode}"
            } );

            if ( typeResponse.FtpStatusCode != FtpStatusCode.CommandOK )
                throw new FtpException( typeResponse.ResponseMessage );
        }

        /// <summary>
        /// Determines the file size of the given file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<long> GetFileSizeAsync( string fileName )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Getting file size for {fileName}" );
            var sizeResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = FtpCommand.SIZE,
                Data = fileName
            } );

            if ( sizeResponse.FtpStatusCode != FtpStatusCode.FileStatus )
                throw new FtpException( sizeResponse.ResponseMessage );

            long fileSize = long.Parse( sizeResponse.ResponseMessage.Substring( 4 ) );
            return fileSize;
        }


        /// <summary>
        /// Sends a command to the FTP server, and returns the response
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public async Task<FtpResponse> SendCommandAsync( FtpCommandEnvelope envelope )
        {
            string commandString = envelope.GetCommandString();
            return await SendCommandAsync(commandString);
        }

        /// <summary>
        /// Sends a command to the FTP server, and returns the response
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<FtpResponse> SendCommandAsync( FtpCommand command )
        {
            return await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = command
            } );
        }

        public async Task<FtpResponse> SendCommandAsync(string command)
        {
            await commandSemaphore.WaitAsync();

            if (HasResponsePending())
            {
                await GetResponseAsync();
            }

            Logger?.LogDebug($"[FtpClient] Sending command: {command}");
            byte[] data = Encoding.GetBytes($"{command}\r\n");
            commandSocket.Send(data);

            var response = await GetResponseAsync();

            commandSemaphore.Release();
            return response;
        }

        public bool HasResponsePending()
        {
            return commandSocket.Connected && commandSocket.Available > 0;
        }

        /// <summary>
        /// Retrieves the latest response from the FTP server on the command socket
        /// </summary>
        /// <returns></returns>
        public async Task<FtpResponse> GetResponseAsync()
        {
            await Task.Delay( 0 );
            Logger?.LogDebug( "[FtpClient] Getting Response" );

            do
            {
                var buffer = new byte[Constants.BUFFER_SIZE];
                string statusMessage = string.Empty;

                while ( true )
                {
                    int byteCount = commandSocket.Receive( buffer, buffer.Length, SocketFlags.None );
                    if ( byteCount == 0 )
                        break;

                    statusMessage += Encoding.ASCII.GetString( buffer, 0, byteCount );

                    if ( byteCount < buffer.Length )
                        break;
                }

                var message = statusMessage.Split( Constants.LINEFEED );

                statusMessage = statusMessage.Length > 2
                    ? message[ message.Length - 2 ]
                    : message[ 0 ];

                if ( !statusMessage.Substring( 3, 1 ).Equals( " " ) )
                    continue;

                Logger?.LogDebug( $"[FtpClient] {statusMessage}" );

                return new FtpResponse
                {
                    ResponseMessage = statusMessage,
                    FtpStatusCode = statusMessage.ToStatusCode(),
                    Data = message
                };
            } while ( true );
        }

        private IDirectoryProvider DetermineDirectoryProvider()
        {
            Logger?.LogDebug( "[FtpClient] Determining directory provider" );
            if ( this.UsesMlsd() )
                return new MlsdDirectoryProvider( this, Logger, Configuration );

            return new ListDirectoryProvider( this, Logger, Configuration );
        }

        private async Task<IEnumerable<string>> DetermineFeaturesAsync()
        {
            EnsureLoggedIn();
            Logger?.LogDebug( "[FtpClient] Determining features" );
            var response = await SendCommandAsync( FtpCommand.FEAT );

            if ( response.FtpStatusCode == FtpStatusCode.CommandSyntaxError || response.FtpStatusCode == FtpStatusCode.CommandNotImplemented )
                return Enumerable.Empty<string>();

            var features = response.Data.Where( x => !x.StartsWith( ( (int) FtpStatusCode.SystemHelpReply ).ToString() ) && !x.IsNullOrWhiteSpace() )
                                   .Select( x => x.Replace( Constants.CARRIAGE_RETURN, string.Empty ).Trim() )
                                   .ToList();


            foreach ( string feature in features )
                Logger?.LogDebug( feature );


            return features;
        }

        /// <summary>
        /// Creates a directory structure recursively given a path
        /// </summary>
        /// <param name="directories"></param>
        /// <param name="isRootedPath"></param>
        /// <returns></returns>
        private async Task CreateDirectoryStructureRecursively( IReadOnlyCollection<string> directories, bool isRootedPath )
        {
            Logger?.LogDebug( $"[FtpClient] Creating directory structure recursively {string.Join( "/", directories )}" );
            string originalPath = WorkingDirectory;

            if ( isRootedPath && directories.Any())
                await ChangeWorkingDirectoryAsync( "/" );

            if ( !directories.Any() )
                return;

            if ( directories.Count == 1 )
            {
                await SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.MKD,
                    Data = directories.First()
                } );

                await ChangeWorkingDirectoryAsync( originalPath );
                return;
            }

            foreach ( string directory in directories )
            {
                if ( directory.IsNullOrWhiteSpace() )
                    continue;

                var response = await SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.CWD,
                    Data = directory
                } );

                if ( response.FtpStatusCode != FtpStatusCode.ActionNotTakenFileUnavailable )
                    continue;

                await SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.MKD,
                    Data = directory
                } );
                await SendCommandAsync( new FtpCommandEnvelope
                {
                    FtpCommand = FtpCommand.CWD,
                    Data = directory
                } );
            }

            await ChangeWorkingDirectoryAsync( originalPath );
        }


        /// <summary>
        /// Opens a filestream to the given filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<Stream> OpenFileStreamAsync( string fileName, FtpCommand command )
        {
            EnsureLoggedIn();
            Logger?.LogDebug( $"[FtpClient] Opening filestream for {fileName}, {command}" );
            dataSocket = await ConnectDataSocketAsync();

            var retrResponse = await SendCommandAsync( new FtpCommandEnvelope
            {
                FtpCommand = command,
                Data = fileName
            } );

            if ( ( retrResponse.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) &&
                 ( retrResponse.FtpStatusCode != FtpStatusCode.OpeningData ) &&
                 ( retrResponse.FtpStatusCode != FtpStatusCode.ClosingData ) )
                throw new FtpException( retrResponse.ResponseMessage );

            return new NetworkStream( dataSocket );
        }

        /// <summary>
        /// Checks if the command socket is open and that an authenticated session is active
        /// </summary>
        private void EnsureLoggedIn()
        {
            if ( !IsConnected || !IsAuthenticated )
                throw new FtpException( "User must be logged in" );
        }

        /// <summary>
        /// Produces a command socket to the FTP server
        /// </summary>
        /// <returns></returns>
        private async Task ConnectCommandSocketAsync()
        {
            try
            {
                Logger?.LogDebug( $"Connecting command socket, {Configuration.Host}:{Configuration.Port}" );

                var ipEndpoint = await dnsResolver.ResolveAsync( Configuration.Host, Configuration.Port, Configuration.IpVersion );

                commandSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp )
                {
                    ReceiveTimeout = Configuration.TimeoutSeconds * 1000
                };
                commandSocket.Connect( ipEndpoint );

                var response = await GetResponseAsync();
                await BailIfResponseNotAsync( response, FtpStatusCode.SendUserCommand );
            }
            catch ( Exception ex )
            {
                Logger?.LogDebug( "Connecting to command socket failed" );
                await LogOutAsync();
                throw new FtpException( "Unable to login to FTP server", ex );
            }
        }

        /// <summary>
        /// Produces a data socket using Extended Passive mode
        /// </summary>
        /// <returns></returns>
        internal async Task<Socket> ConnectDataSocketAsync()
        {
            Logger?.LogDebug( "[FtpClient] Connecting to a data socket" );
            var epsvResult = await SendCommandAsync( FtpCommand.EPSV );

            if ( epsvResult.FtpStatusCode != FtpStatusCode.EnteringExtendedPassive )
                throw new FtpException( epsvResult.ResponseMessage );

            var passivePortNumber = epsvResult.ResponseMessage.ExtractEpsvPortNumber();
            if ( !passivePortNumber.HasValue )
                throw new FtpException( "Could not detmine EPSV data port" );


            Socket socket = null;
            try
            {
                Logger?.LogDebug( $"Connecting data socket, {Configuration.Host}:{passivePortNumber.Value}" );

                var ipEndpoint = await dnsResolver.ResolveAsync( Configuration.Host, passivePortNumber.Value, Configuration.IpVersion );

                socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp )
                {
                    ReceiveTimeout = Configuration.TimeoutSeconds * 1000
                };
                socket.Connect( ipEndpoint );

                return socket;
            }
            catch ( Exception ex )
            {
                if ( socket != null && socket.Connected )
                    socket.Shutdown( SocketShutdown.Both );
                throw new FtpException( "Can't connect to remote server", ex );
            }
        }

        /// <summary>
        /// Throws an exception if the server response is not one of the given acceptable codes
        /// </summary>
        /// <param name="response"></param>
        /// <param name="codes"></param>
        /// <returns></returns>
        private async Task BailIfResponseNotAsync( FtpResponse response, params FtpStatusCode[] codes )
        {
            if ( codes.Any( x => x == response.FtpStatusCode ) )
                return;

            Logger?.LogDebug( $"Bailing due to response codes not being one of: [{string.Join( ",", codes )}]" );

            await LogOutAsync();
            throw new FtpException( response.ResponseMessage );
        }

        public void Dispose()
        {
            Logger?.LogDebug( "Disposing of resources" );
            Task.WaitAny( LogOutAsync(), Task.Delay( 5000 ) );
            commandSemaphore.Release();
            commandSocket?.Dispose();
            dataSocket?.Dispose();
        }
    }
}