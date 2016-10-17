namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using Enum;
    using Xunit;

    public class When_changing_transfer_type
    {
        public When_changing_transfer_type()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_set_as_ascii_with_and_without_second_type()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetTransferMode( FtpTransferMode.Ascii );
                await sut.SetTransferMode( FtpTransferMode.Ascii, 'N' );
                await sut.SetTransferMode( FtpTransferMode.Ascii, 'T' );
                await sut.SetTransferMode( FtpTransferMode.Ascii, 'C' );
            }
        }

        [ Fact ]
        public async Task Should_set_as_binary()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetTransferMode( FtpTransferMode.Binary );
            }
        }
    }
}