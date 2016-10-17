namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_listing_files
    {
        public When_listing_files()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_list_files_in_root()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string randomFileName = $"{Guid.NewGuid()}.jpg";
                await sut.LoginAsync();
                await sut.CreateTestResourceWithNameAsync( "penguin.jpg", randomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeFalse();
            }
        }

        [ Fact ]
        public async Task Should_list_files_in_subdirectory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string randomDirectoryName = $"{Guid.NewGuid()}";
                string randomFileName = $"{Guid.NewGuid()}.jpg";

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryName );
                await sut.CreateTestResourceWithNameAsync( "penguin.jpg", randomFileName );
                var files = await sut.ListFilesAsync();

                files.Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );
                await sut.ChangeWorkingDirectoryAsync( "../" );
                await sut.DeleteDirectoryAsync( randomDirectoryName );
            }
        }
    }
}