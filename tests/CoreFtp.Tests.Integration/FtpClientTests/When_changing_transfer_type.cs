namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using Enum;
    using Xunit;

    public class When_changing_transfer_type
    {
        [ Fact ]
        public async Task Should_set_as_ascii_with_and_without_second_type()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
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
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetTransferMode( FtpTransferMode.Binary );
            }
        }
    }
}