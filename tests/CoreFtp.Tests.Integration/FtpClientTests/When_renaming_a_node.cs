namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_renaming_a_node
    {
        [ Fact ]
        public async Task Should_rename_directory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                string oldRandomDirectoryName = Guid.NewGuid().ToString();
                string newRandomDirectoryName = Guid.NewGuid().ToString();

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( oldRandomDirectoryName );


                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == oldRandomDirectoryName ).Should().BeTrue();

                await sut.RenameAsync( oldRandomDirectoryName, newRandomDirectoryName );

                var directoriesAfterRename = ( await sut.ListDirectoriesAsync() );
                directoriesAfterRename.Any( x => x.Name == oldRandomDirectoryName ).Should().BeFalse();
                directoriesAfterRename.Any( x => x.Name == newRandomDirectoryName ).Should().BeTrue();

                await sut.DeleteDirectoryAsync( newRandomDirectoryName );
                await sut.LogOutAsync();
            }
        }

        [ Fact ]
        public async Task Should_rename_file()
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

                var originalFilename = "original_penguin.jpg";
                var newFilename = $"renamed_penguin{Guid.NewGuid()}.jpg";

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( originalFilename ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                ( await sut.ListFilesAsync() ).Any( x => x.Name == originalFilename ).Should().BeTrue();


                await sut.RenameAsync( originalFilename, newFilename );

                var filesAfterRename = await sut.ListFilesAsync();
                filesAfterRename.Any( x => x.Name == originalFilename ).Should().BeFalse();
                filesAfterRename.Any( x => x.Name == newFilename ).Should().BeTrue();

                await sut.DeleteFileAsync( newFilename );
            }
        }
    }
}