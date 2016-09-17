namespace CoreFtp
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;
    using Enum;
    using Extensions;

    public class FtpClient : IDisposable
    {
        private const string ANONYMOUS_USER = "anonymous";
        private const char LINEFEED = '\n';
        private const string CARRIAGE_RETURN = "\r";

        private static readonly int BUFFER_SIZE = 512;
        private Socket commandSocket { get; set; }
        private Socket dataSocket { get; set; }
        public bool IsConnected => ( commandSocket != null ) && commandSocket.Connected;
        public bool IsAuthenticated { get; set; }

        public string WorkingDirectory { get; set; }
        private readonly FtpClientConfiguration configuration;

        public FtpClient( FtpClientConfiguration configuration )
        {
            this.configuration = configuration;

            if ( configuration.Host == null )
                throw new ArgumentNullException( nameof( configuration.Host ) );
        }

        /// <summary>
        ///     Attempts to log the user in to the FTP Server
        /// </summary>
        /// <returns></returns>
        public async Task LoginAsync()
        {
            if ( IsConnected )
                await LogOutAsync();

            string username = configuration.Username.IsNullOrWhiteSpace() ? ANONYMOUS_USER : configuration.Username;

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
                                                           Data = username != ANONYMOUS_USER ? configuration.Password : string.Empty
                                                       } );

            await BailIfResponseNotAsync( passResponse, FtpStatusCode.LoggedInProceed );
            IsAuthenticated = true;

            if ( configuration.BaseDirectory != "/" )
            {
                await ChangeWorkingDirectoryAsync( configuration.BaseDirectory );
            }
        }

        /// <summary>
        ///     Attemps to log the user out asynchronously, sends the QUIT command and terminates the command socket.
        /// </summary>
        public async Task LogOutAsync()
        {
            await Task.Run( async () =>
                            {
                                if ( !IsConnected )
                                    return;

                                await SendCommandAsync( FtpCommand.QUIT );
                                commandSocket.Shutdown( SocketShutdown.Both );
                                IsAuthenticated = false;
                            } );
        }

        /// <summary>
        /// Changes the working directory to the given value for the current session
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task ChangeWorkingDirectoryAsync( string directory )
        {
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

            WorkingDirectory = response.ResponseMessage.Split( '"' )[ 1 ];
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

            EnsureLoggedIn();

            var response = await SendCommandAsync( new FtpCommandEnvelope
                                                   {
                                                       FtpCommand = FtpCommand.MKD,
                                                       Data = directory
                                                   } );

            if ( response.FtpStatusCode != FtpStatusCode.FileActionOK && response.FtpStatusCode != FtpStatusCode.PathnameCreated )
                throw new FtpException( response.ResponseMessage );
        }

        public async Task RenameAsync( string from, string to )
        {
            EnsureLoggedIn();

            var rnfrResponse = await SendCommandAsync( new FtpCommandEnvelope
                                                       {
                                                           FtpCommand = FtpCommand.RNFR,
                                                           Data = from
                                                       } );

            if ( rnfrResponse.FtpStatusCode != FtpStatusCode.FileCommandPending )
                throw new FtpException( rnfrResponse.ResponseMessage );

            var rntoResponse = await SendCommandAsync( new FtpCommandEnvelope
                                                       {
                                                           FtpCommand = FtpCommand.RNTO,
                                                           Data = to
                                                       } );

            if ( rntoResponse.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( rnfrResponse.ResponseMessage );
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

            EnsureLoggedIn();

            var response = await SendCommandAsync( new FtpCommandEnvelope
                                                   {
                                                       FtpCommand = FtpCommand.RMD,
                                                       Data = directory
                                                   } );

            if ( response.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( response.ResponseMessage );
        }

        /// <summary>
        /// Informs the FTP server of the client being used
        /// </summary>
        /// <param name="clientName"></param>
        /// <returns></returns>
        public async Task<FtpResponse> SetClientName( string clientName )
        {
            EnsureLoggedIn();
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
            return await OpenFileStreamAsync( fileName, FtpCommand.RETR );
        }

        /// <summary>
        /// Provides a stream which can be written to
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<Stream> OpenFileWriteStreamAsync( string fileName )
        {
            return await OpenFileStreamAsync( fileName, FtpCommand.STOR );
        }

        /// <summary>
        /// Closes the write stream and associated socket (if open)
        /// </summary>
        /// <returns></returns>
        public async Task CloseFileWriteStreamAsync()
        {
            if ( !dataSocket.Connected )
                return;

            dataSocket.Shutdown( SocketShutdown.Both );
            await GetResponseAsync();
        }

        /// <summary>
        /// Lists all files in the current working directory
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<string>> ListFilesAsync()
        {
            return await ListNodeTypeAsync( FtpNodeType.File );
        }

        /// <summary>
        /// Lists all directories in the current working directory
        /// </summary>
        /// <returns></returns>
        public async Task<ReadOnlyCollection<string>> ListDirectoriesAsync()
        {
            return await ListNodeTypeAsync( FtpNodeType.Directory );
        }


        /// <summary>
        /// Lists all directories in the current working directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task DeleteFileAsync( string fileName )
        {
            EnsureLoggedIn();
            var deleResponse = await SendCommandAsync( new FtpCommandEnvelope
                                                       {
                                                           FtpCommand = FtpCommand.DELE,
                                                           Data = fileName
                                                       } );

            if ( deleResponse.FtpStatusCode != FtpStatusCode.FileActionOK )
                throw new FtpException( deleResponse.ResponseMessage );
        }


        /// <summary>
        /// Sends a command to the FTP server, and returns the response
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public async Task<FtpResponse> SendCommandAsync( FtpCommandEnvelope envelope )
        {
            await Task.Run( () =>
                            {
                                var commandInBytes = envelope.GetCommandString().ToAsciiBytes();
                                commandSocket.Send( commandInBytes );
                            } );

            return await GetResponseAsync();
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

        /// <summary>
        /// Lists all directories in the current working directory
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<Stream> OpenFileStreamAsync( string fileName, FtpCommand command )
        {
            EnsureLoggedIn();

            dataSocket = await ConnectDataSocketAsync();

            var retrResponse = await SendCommandAsync( new FtpCommandEnvelope
                                                       {
                                                           FtpCommand = command,
                                                           Data = fileName
                                                       } );

            if ( ( retrResponse.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( retrResponse.FtpStatusCode != FtpStatusCode.OpeningData ) )
                throw new FtpException( retrResponse.ResponseMessage );

            return new NetworkStream( dataSocket );
        }

        /// <summary>
        /// Lists all nodes (files and directories) in the current working directory
        /// </summary>
        /// <param name="ftpNodeType"></param>
        /// <returns></returns>
        private async Task<ReadOnlyCollection<string>> ListNodeTypeAsync( FtpNodeType ftpNodeType )
        {
            string nodeTypeString = ftpNodeType == FtpNodeType.File
                ? "file"
                : "dir";

            EnsureLoggedIn();

            dataSocket = await ConnectDataSocketAsync();

            if ( dataSocket == null )
                throw new FtpException( "Could not establish a data connection" );

            var mlsdResult = await SendCommandAsync( FtpCommand.MLSD );

            if ( ( mlsdResult.FtpStatusCode != FtpStatusCode.DataAlreadyOpen ) && ( mlsdResult.FtpStatusCode != FtpStatusCode.OpeningData ) )
                throw new FtpException( "Could not list files, " + mlsdResult.ResponseMessage );

            var maxTime = DateTime.Now.AddSeconds( configuration.TimeoutSeconds );
            bool hasTimedOut;

            string csv = string.Empty;

            do
            {
                var buffer = new byte[BUFFER_SIZE];

                int byteCount = dataSocket.Receive( buffer, buffer.Length, 0 );
                csv += Encoding.ASCII.GetString( buffer, 0, byteCount );

                if ( byteCount == 0 ) break;

                hasTimedOut = ( configuration.TimeoutSeconds == 0 ) || ( DateTime.Now < maxTime );
            } while ( hasTimedOut );

            dataSocket.Shutdown( SocketShutdown.Both );

            await GetResponseAsync();

            var nodes = csv.Replace( CARRIAGE_RETURN, string.Empty )
                           .Split( LINEFEED )
                           .Where( x => x.Contains( $"type={nodeTypeString}" ) )
                           .Select( x => x.Split( ';' ).Last().Trim() );


            return nodes.ToList().AsReadOnly();
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
                await Task.Run( () =>
                                {
                                    commandSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                                    commandSocket.Connect( configuration.Host, configuration.Port );
                                } );
                var response = await GetResponseAsync();
                await BailIfResponseNotAsync( response, FtpStatusCode.SendUserCommand );
            }
            catch ( Exception ex )
            {
                await LogOutAsync();
                throw new FtpException( "Unable to login to FTP server", ex );
            }
        }

        /// <summary>
        /// Produces a data socket using Extended Passive mode
        /// </summary>
        /// <returns></returns>
        private async Task<Socket> ConnectDataSocketAsync()
        {
            var epsvResult = await SendCommandAsync( FtpCommand.EPSV );

            if ( epsvResult.FtpStatusCode != FtpStatusCode.EnteringExtendedPassive )
                throw new FtpException( epsvResult.ResponseMessage );

            var passivePortNumber = epsvResult.ResponseMessage.ExtractEpsvPortNumber();
            if ( !passivePortNumber.HasValue )
                throw new FtpException( "Could not detmine EPSV data port" );


            Socket socket = null;
            try
            {
                socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                socket.Connect( configuration.Host, passivePortNumber.Value );

                return socket;
            }
            catch ( Exception ex )
            {
                if ( ( socket != null ) && socket.Connected )
                    socket.Shutdown( SocketShutdown.Both );
                throw new FtpException( "Can't connect to remote server", ex );
            }
        }

        /// <summary>
        /// Throws an exception if the server response is not one of the given acceptable codes
        /// </summary>
        /// <param name="response"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private async Task BailIfResponseNotAsync( FtpResponse response, params FtpStatusCode[] code )
        {
            if ( code.Any( x => x == response.FtpStatusCode ) )
                return;

            await LogOutAsync();
            throw new FtpException( response.ResponseMessage );
        }

        /// <summary>
        /// Retrieves the latest response from the FTP server on the command socket
        /// </summary>
        /// <returns></returns>
        private async Task<FtpResponse> GetResponseAsync()
        {
            return await Task.Run( () =>
                                   {
                                       do
                                       {
                                           var buffer = new byte[BUFFER_SIZE];
                                           string statusMessage = string.Empty;

                                           while ( true )
                                           {
                                               int byteCount = commandSocket.Receive( buffer, buffer.Length, 0 );
                                               statusMessage += Encoding.ASCII.GetString( buffer, 0, byteCount );

                                               if ( byteCount < buffer.Length )
                                                   break;
                                           }

                                           var message = statusMessage.Split( LINEFEED );

                                           statusMessage = statusMessage.Length > 2
                                               ? message[ message.Length - 2 ]
                                               : message[ 0 ];

                                           if ( !statusMessage.Substring( 3, 1 ).Equals( " " ) )
                                               continue;

                                           return new FtpResponse
                                           {
                                               ResponseMessage = statusMessage,
                                               FtpStatusCode = statusMessage.ToStatusCode()
                                           };
                                       } while ( true );
                                   } );
        }


        public void Dispose()
        {
            LogOutAsync().GetAwaiter().GetResult();
            commandSocket?.Dispose();
            dataSocket?.Dispose();
        }
    }
}