namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Infrastructure;
    using Xunit;

    public class When_changing_working_directories
    {
        [ Fact ]
        public async Task Should_fail_when_changing_to_a_nonexistent_directory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_fail_when_changing_to_a_nonexistent_directory ) );
                await Assert.ThrowsAsync<FtpException>( () => sut.ChangeWorkingDirectoryAsync( Guid.NewGuid().ToString() ) );
            }
        }

        [ Fact ]
        public async Task Should_change_to_directory_when_exists()
        {
            string randomDirectoryName = Guid.NewGuid().ToString();

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryName );
                sut.WorkingDirectory.Should().Be( $"/{randomDirectoryName}" );

                await sut.ChangeWorkingDirectoryAsync( "../" );
                await sut.DeleteDirectoryAsync( randomDirectoryName );
            }
        }

        [ Fact ]
        public async Task Should_change_to_deep_directory_when_exists()
        {
            string[] randomDirectoryNames =
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                string joinedPath = string.Join( "/", randomDirectoryNames );
                await sut.LoginAsync();

                await sut.CreateDirectoryAsync( joinedPath );
                await sut.ChangeWorkingDirectoryAsync( joinedPath );
                sut.WorkingDirectory.Should().Be( $"/{joinedPath}" );

                foreach ( string directory in randomDirectoryNames.Reverse() )
                {
                    await sut.ChangeWorkingDirectoryAsync( "../" );
                    await sut.DeleteDirectoryAsync( directory );
                }
            }
        }
    }
}