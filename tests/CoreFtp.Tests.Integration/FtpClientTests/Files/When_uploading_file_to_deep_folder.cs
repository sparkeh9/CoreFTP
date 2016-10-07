namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_uploading_file_to_deep_folder
    {
        [ Fact ]
        public async Task Should_recurse_as_appropriate_to_create_file()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();


                var newGuid = Guid.NewGuid();
                using ( var writeStream = await sut.OpenFileWriteStreamAsync( $"folder1/folder2/{newGuid}.txt" ) )
                {
                    var fileReadStream = "abc123".ToByteStream();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == "folder1" ).Should().BeTrue();
                await sut.ChangeWorkingDirectoryAsync( "folder1" );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == "folder2" ).Should().BeTrue();
                await sut.ChangeWorkingDirectoryAsync( "folder2" );
                ( await sut.ListFilesAsync() ).Any( x => x.Name == $"{newGuid}.txt" ).Should().BeTrue();
            }
        }
    }
}