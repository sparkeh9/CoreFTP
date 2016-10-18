namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Infrastructure;
    using Xunit;

    public class When_deleting_directories : TestBase
    {
        [ Fact ]
        public async Task Should_throw_exception_when_folder_nonexistent()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                sut.Logger = Logger;

                string randomDirectoryName = Guid.NewGuid().ToString();
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.DeleteDirectoryAsync( randomDirectoryName ) );
                await sut.LogOutAsync();
            }
        }

        [ Fact ]
        public async Task Should_delete_directory_when_exists()
        {
            string randomDirectoryName = Guid.NewGuid().ToString();

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
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeTrue();
                await sut.DeleteDirectoryAsync( randomDirectoryName );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeFalse();
            }
        }
    }
}