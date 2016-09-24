namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_uploading_a_file
    {
        [ Fact ]
        public async Task Should_upload_file()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.ChangeWorkingDirectoryAsync( "test2" );
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( "uploaded_penguin.jpg" ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                    await sut.CloseFileWriteStreamAsync();
                }

                var files = await sut.ListFilesAsync();

                files.Any( x => x.Name == "uploaded_penguin.jpg" ).Should().BeTrue();
            }
        }

        [ Fact ]
        public async Task Should_upload_file_to_subdirectory()
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
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( "uploaded_penguin.jpg" ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                    await sut.CloseFileWriteStreamAsync();

                    var files = await sut.ListFilesAsync();

                    files.Any( x => x.Name == "uploaded_penguin.jpg" ).Should().BeTrue();
                }
            }
        }
    }
}