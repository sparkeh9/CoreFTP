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
        public When_uploading_file_to_deep_folder()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_recurse_as_appropriate_to_create_file()
        {
            string[] randomDirectoryNames =
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
            string joinedPath = string.Join( "/", randomDirectoryNames );

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                await sut.LoginAsync();


                string randomFile = $"{Guid.NewGuid()}.txt";
                using ( var writeStream = await sut.OpenFileWriteStreamAsync( $"/{joinedPath}/{randomFile}" ) )
                {
                    var fileReadStream = "abc123".ToByteStream();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryNames[ 0 ] ).Should().BeTrue();
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryNames[ 0 ] );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryNames[ 1 ] ).Should().BeTrue();
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryNames[ 1 ] );
                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFile ).Should().BeTrue();


                await sut.DeleteFileAsync( randomFile );

                foreach ( string directory in randomDirectoryNames.Reverse() )
                {
                    await sut.ChangeWorkingDirectoryAsync( "../" );
                    await sut.DeleteDirectoryAsync( directory );
                }
            }
        }
    }
}