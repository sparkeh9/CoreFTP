namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using Enum;
    using Xunit;
    using Xunit.Abstractions;

    public class When_using_a_uri_as_hostname : TestBase
    {
        public When_using_a_uri_as_hostname( ITestOutputHelper outputHelper ) : base( outputHelper ) { }

        [ Fact ]
        public async Task Should_extract_hostname_and_work_as_normal()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = $"ftp://{Program.FtpConfiguration.Host}/",
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = 990,
                EncryptionType = FtpEncryption.Implicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                await sut.LoginAsync();
                await sut.LogOutAsync();
            }
        }
    }
}