namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using System.Linq;

    public class When_creating_directories
    {
        [ Fact ]
        public async Task Should_create_a_directory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                string randomDirectoryName = Guid.NewGuid().ToString();
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                var directories = await sut.ListDirectoriesAsync();
                await sut.DeleteDirectoryAsync( randomDirectoryName );
                await sut.LogOutAsync();

                directories.Any( x => x == randomDirectoryName ).Should().BeTrue();
            }
        }
    }
}