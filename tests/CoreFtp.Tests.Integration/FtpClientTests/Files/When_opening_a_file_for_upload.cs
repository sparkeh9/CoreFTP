namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Infrastructure.Extensions;

    public class When_uploading_a_file : TestBase
    {
        public When_uploading_a_file( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Fact ]
        public async Task Should_upload_file()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port
            } ) )
            {
                sut.Logger = Logger;
                string randomFileName = $"{Guid.NewGuid()}.jpg";

                await sut.LoginAsync();
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( randomFileName ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                var files = await sut.ListFilesAsync();

                files.Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );
                await sut.ChangeWorkingDirectoryAsync( "/" );
                await sut.DeleteDirectoryAsync( sut.Configuration.BaseDirectory );
            }
        }

        [ Fact ]
        public async Task Should_upload_file_to_subdirectory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port
            } ) )
            {
                sut.Logger = Logger;
                string randomDirectoryName = $"{Guid.NewGuid()}";
                string randomFileName = $"{Guid.NewGuid()}.jpg";

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( $"/{randomDirectoryName}/{randomFileName}" ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                await sut.ChangeWorkingDirectoryAsync( randomDirectoryName );

                var files = await sut.ListFilesAsync();
                files.Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );
                await sut.ChangeWorkingDirectoryAsync( "../" );
                await sut.DeleteDirectoryAsync( randomDirectoryName );
            }
        }

        [ Fact ]
        public async Task Should_upload_file_to_subdirectory_when_given_as_basepath()
        {
            var guid = Guid.NewGuid();
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port,
                BaseDirectory = $"{guid}/abc/123/doraeme"
            } ) )
            {
                sut.Logger = Logger;
                string randomFileName = $"{guid}.jpg";

                await sut.LoginAsync();
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( $"another_level/{randomFileName}" ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                await sut.ChangeWorkingDirectoryAsync( "/".CombineAsUriWith( sut.Configuration.BaseDirectory )
                                                          .CombineAsUriWith( "another_level" ) );

                var files = await sut.ListFilesAsync();
                files.Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteDirectoryAsync( $"/{guid}" );
            }
        }
    }
}