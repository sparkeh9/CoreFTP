namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using Xunit.Abstractions;

    public class When_providing_a_base_directory : TestBase
    {
        public When_providing_a_base_directory( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Fact ]
        public async Task Should_be_in_base_directory_when_logging_in()
        {
            string randomDirectoryName = $"{Guid.NewGuid()}";

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                sut.Logger = Logger;
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
            }

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port,
                                                 BaseDirectory = randomDirectoryName
                                             } ) )
            {
                sut.Logger = Logger;
                await sut.LoginAsync();
                sut.WorkingDirectory.Should().Be( $"/{randomDirectoryName}" );
                await sut.DeleteDirectoryAsync( $"/{randomDirectoryName}" );
            }
        }
    }
}