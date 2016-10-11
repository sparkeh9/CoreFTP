namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
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
                string randomFileName = $"{Guid.NewGuid()}.jpg";
                await sut.LoginAsync();
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( randomFileName ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeFalse();
            }
        }
    }
}