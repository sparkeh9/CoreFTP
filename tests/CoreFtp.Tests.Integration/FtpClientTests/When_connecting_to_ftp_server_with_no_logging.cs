namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_connecting_to_ftp_server_with_no_logging : TestBase
    {
        [ Fact ]
        public async Task Should_be_able_to_connect_and_disconnect_as_user_with_account()
        {
            var ftpClient = new FtpClient( new FtpClientConfiguration
                                           {
                                               Host = "localhost",
                                               Port = 21,
                                               Username = "user",
                                               Password = "password"
                                           } );

            var directories = await ftpClient.ListDirectoriesAsync();
            var files = await ftpClient.ListFilesAsync();

            await ftpClient.CreateDirectoryAsync( "testy" );
            await ftpClient.LogOutAsync();
        }
    }
}