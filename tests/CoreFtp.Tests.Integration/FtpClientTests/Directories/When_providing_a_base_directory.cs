namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_providing_a_base_directory
    {
        [ Fact ]
        public async Task Should_be_in_base_directory_when_logging_in()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password",
                                                 BaseDirectory = "test1"
                                             } ) )
            {
                await sut.LoginAsync();
                sut.WorkingDirectory.Should().Be( "/test1" );
            }
        }
    }
}