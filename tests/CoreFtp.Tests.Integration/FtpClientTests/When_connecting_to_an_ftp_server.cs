namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;

    public class When_connecting_to_an_ftp_server : TestBase
    {
        public When_connecting_to_an_ftp_server( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_be_able_to_connect_and_disconnect_as_anonymous_user( FtpEncryption encryption )
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;
                sut.IsConnected.Should().BeFalse();
                await sut.LoginAsync();
                sut.IsConnected.Should().BeTrue();
                await sut.LogOutAsync();
                sut.IsConnected.Should().BeFalse();
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_be_able_to_connect_and_disconnect_as_user_with_account( FtpEncryption encryption )
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;
                sut.IsConnected.Should().BeFalse();
                await sut.LoginAsync();
                sut.IsConnected.Should().BeTrue();
                await sut.LogOutAsync();
                sut.IsConnected.Should().BeFalse();
            }
        }
    }
}