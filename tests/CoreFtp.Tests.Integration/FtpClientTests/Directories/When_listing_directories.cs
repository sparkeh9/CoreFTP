namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using System.Linq;

    public class When_listing_directories
    {
        [ Fact ]
        public async Task Should_list_directories_in_root()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                var files = await sut.ListDirectoriesAsync();
                await sut.LogOutAsync();


                files.Any(x => x == "test1" ).Should().BeTrue();
                files.Any(x => x == "test2" ).Should().BeTrue();
            }
        }

        [ Fact ]
        public async Task Should_list_directories_in_subdirectory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.ChangeWorkingDirectoryAsync( "test1" );
                var files = await sut.ListDirectoriesAsync();
                await sut.LogOutAsync();

                files.Count.Should().Be( 2 );
            }
        }
    }
}