namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class When_listing_files : TestBase
    {
        public When_listing_files( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_list_files_in_root( FtpEncryption encryption )
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;
                string randomFileName = $"{Guid.NewGuid()}.jpg";
                await sut.LoginAsync();
                await sut.CreateTestResourceWithNameAsync( "penguin.jpg", randomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeTrue();

                await sut.DeleteFileAsync( randomFileName );

                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeFalse();
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_list_files_in_subdirectory( FtpEncryption encryption )
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;
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