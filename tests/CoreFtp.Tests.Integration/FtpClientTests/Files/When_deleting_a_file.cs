namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_deleting_a_file
    {
        [ Fact ]
        public async Task Should_delete_file()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( "uploaded_penguin.jpg" ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                    await sut.CloseFileWriteStreamAsync();
                }

                ( await sut.ListFilesAsync() ).Any( x => x.Name == "uploaded_penguin.jpg" ).Should().BeTrue();

                await sut.DeleteFileAsync( "uploaded_penguin.jpg" );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == "uploaded_penguin.jpg" ).Should().BeFalse();
            }
        }
    }
}