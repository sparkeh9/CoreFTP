namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;

    public class When_connecting_to_ftp_server_with_no_logging : TestBase
    {
        public When_connecting_to_ftp_server_with_no_logging( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Fact ]
        public async Task Should_be_able_to_connect_and_disconnect_as_user_with_account()
        {
            var ftpClient = new FtpClient( new FtpClientConfiguration
                                           {
                                               Host = Program.FtpConfiguration.Host,
                                               Port = Program.FtpConfiguration.Port,
                                               Username = Program.FtpConfiguration.Username,
                                               Password = Program.FtpConfiguration.Password
                                           } );

            await ftpClient.LoginAsync();

            var directories = await ftpClient.ListDirectoriesAsync();
            var files = await ftpClient.ListFilesAsync();

            await ftpClient.CreateDirectoryAsync( "testy" );
            await ftpClient.LogOutAsync();
        }
    }
}