namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_providing_a_base_directory
    {
        [ Fact ]
        public async Task Should_be_in_base_directory_when_logging_in()
        {
            string randomDirectoryName = $"{Guid.NewGuid()}";

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
            }

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password",
                                                 BaseDirectory = randomDirectoryName
                                             } ) )
            {
                await sut.LoginAsync();
                sut.WorkingDirectory.Should().Be( $"/{randomDirectoryName}" );
                await sut.DeleteDirectoryAsync( $"/{randomDirectoryName}" );
            }
        }
    }
}