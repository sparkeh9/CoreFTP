namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Enum;
    using Xunit;
    using Xunit.Abstractions;

    public class When_sending_a_custom_command : TestBase
    {
        public When_sending_a_custom_command( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Fact ]
        public async Task Should_send_custom_command()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = 990,
                EncryptionType = FtpEncryption.Implicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                await sut.LoginAsync();
                var response = await sut.SendCommandAsync( "FEAT" );
                response.Data.Contains( "UTF8" );
                await sut.LogOutAsync();
            }
        }
    }
}