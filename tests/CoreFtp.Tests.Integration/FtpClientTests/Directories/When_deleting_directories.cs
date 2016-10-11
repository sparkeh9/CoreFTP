namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_deleting_directories
    {
        [ Fact ]
        public async Task Should_throw_exception_when_folder_nonexistent()
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
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeTrue();
                await sut.DeleteDirectoryAsync( randomDirectoryName );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeFalse();
            }
        }

//        [ Fact ]
//        public async Task Should_delete_recursive_directory_when_exists()
//        {
//            using ( var sut = new FtpClient( new FtpClientConfiguration
//                                             {
//                                                 Host = "localhost",
//                                                 Username = "user",
//                                                 Password = "password"
//                                             } ) )
//            {
//                string topLevelFolder = Guid.NewGuid().ToString();
//                string randomDirectoryName = $"{topLevelFolder}/1/2/3/4/5/6/7/8/9";
//                await sut.LoginAsync();
//                await sut.CreateDirectoryAsync( randomDirectoryName );
//                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == topLevelFolder ).Should().BeTrue();
//                await sut.DeleteDirectoryAsync( topLevelFolder );
//                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == topLevelFolder ).Should().BeFalse();
//            }
//        }
    }
}