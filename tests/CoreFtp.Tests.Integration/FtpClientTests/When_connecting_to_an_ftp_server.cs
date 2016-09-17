namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_connecting_to_an_ftp_server
    {
        [ Fact ]
        public async Task Should_be_able_to_connect_and_disconnect_as_anonymous_user()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                             } ) )
            {
                sut.IsConnected.Should().BeFalse();
                await sut.LoginAsync();
                sut.IsConnected.Should().BeTrue();
                await sut.LogOutAsync();
                sut.IsConnected.Should().BeFalse();
            }
        }

        [ Fact ]
        public async Task Should_be_able_to_connect_and_disconnect_as_user_with_account()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                sut.IsConnected.Should().BeFalse();
                await sut.LoginAsync();
                sut.IsConnected.Should().BeTrue();
                await sut.LogOutAsync();
                sut.IsConnected.Should().BeFalse();
            }
        }
    }
}