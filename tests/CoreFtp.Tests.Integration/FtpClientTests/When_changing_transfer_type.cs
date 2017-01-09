namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using Enum;
    using Xunit;
    using Xunit.Abstractions;

    public class When_changing_transfer_type : TestBase
    {
        public When_changing_transfer_type( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_set_as_ascii_with_and_without_second_type( FtpEncryption encryption )
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
                await sut.LoginAsync();
                await sut.SetTransferMode( FtpTransferMode.Ascii );
                await sut.SetTransferMode( FtpTransferMode.Ascii, 'N' );
                await sut.SetTransferMode( FtpTransferMode.Ascii, 'T' );
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_set_as_binary( FtpEncryption encryption )
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
                await sut.LoginAsync();
                await sut.SetTransferMode( FtpTransferMode.Binary );
            }
        }
    }
}