namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using System.Linq;
    using Helpers;

    public class When_listing_directories
    {
        [ Fact ]
        public async Task Should_list_directories_in_root()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string randomDirectoryName = $"{Guid.NewGuid()}";

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                var directories = await sut.ListDirectoriesAsync();

                directories.Any( x => x.Name == randomDirectoryName ).Should().BeTrue();

                await sut.DeleteDirectoryAsync( randomDirectoryName );
            }
        }

        [ Fact ]
        public async Task Should_list_directories_in_subdirectory()
        {
            string[] randomDirectoryNames =
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string joinedPath = string.Join( "/", randomDirectoryNames );
                await sut.LoginAsync();

                await sut.CreateDirectoryAsync( joinedPath );
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryNames[ 0 ] );
                var directories = await sut.ListDirectoriesAsync();

                directories.Any( x => x.Name == randomDirectoryNames[ 1 ] ).Should().BeTrue();

                await sut.ChangeWorkingDirectoryAsync( $"/{joinedPath}" );
                foreach ( string directory in randomDirectoryNames.Reverse() )
                {
                    await sut.ChangeWorkingDirectoryAsync( "../" );
                    await sut.DeleteDirectoryAsync( directory );
                }
            }
        }
    }
}