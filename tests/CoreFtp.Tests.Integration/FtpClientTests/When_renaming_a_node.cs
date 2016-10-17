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
        public When_renaming_a_node()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_rename_directory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string oldRandomDirectoryName = Guid.NewGuid().ToString();
                string newRandomDirectoryName = Guid.NewGuid().ToString();

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( oldRandomDirectoryName );


                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == oldRandomDirectoryName ).Should().BeTrue();

                await sut.RenameAsync( oldRandomDirectoryName, newRandomDirectoryName );

                var directoriesAfterRename = await sut.ListDirectoriesAsync();
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
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string originalRandomFileName = $"{Guid.NewGuid()}.jpg";
                string subsequentlyRenamedRandomFileName = $"{Guid.NewGuid()}.jpg";

                await sut.LoginAsync();
                await sut.CreateTestResourceWithNameAsync( "penguin.jpg", originalRandomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == originalRandomFileName ).Should().BeTrue();

                await sut.RenameAsync( originalRandomFileName, subsequentlyRenamedRandomFileName );

                var filesAfterRename = await sut.ListFilesAsync();
                filesAfterRename.Any( x => x.Name == originalRandomFileName ).Should().BeFalse();
                filesAfterRename.Any( x => x.Name == subsequentlyRenamedRandomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( subsequentlyRenamedRandomFileName );
            }
        }
    }
}