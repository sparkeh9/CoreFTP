namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_listing_files
    {
        [ Fact ]
        public async Task Should_list_files_in_root()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                var files = await sut.ListFilesAsync();

                files.Any( x => x.Name == "test.png" ).Should().BeTrue();
            }
        }

        [ Fact ]
        public async Task Should_list_files_in_subdirectory()
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
                var files = await sut.ListFilesAsync();

                files.Any( x => x.Name == "test.png" ).Should().BeTrue();
            }
        }
    }
}