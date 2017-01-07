namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using System.Linq;
    using Enum;
    using Xunit.Abstractions;

    public class When_listing_directories : TestBase
    {
        public When_listing_directories( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_list_directories_in_root( FtpEncryption encryption )
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

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                var directories = await sut.ListDirectoriesAsync();

                directories.Any( x => x.Name == randomDirectoryName ).Should().BeTrue();

                await sut.DeleteDirectoryAsync( randomDirectoryName );
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_list_directories_in_subdirectory( FtpEncryption encryption )
        {
            string[] randomDirectoryNames =
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString()
            };
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

                string joinedPath = string.Join( "/", randomDirectoryNames );
                await sut.LoginAsync();

                await sut.CreateDirectoryAsync( joinedPath );
                await sut.ChangeWorkingDirectoryAsync( randomDirectoryNames[ 0 ] );
                var directories = await sut.ListDirectoriesAsync();

                directories.Any( x => x.Name == randomDirectoryNames[ 1 ] ).Should().BeTrue();

                await sut.ChangeWorkingDirectoryAsync( $"/{joinedPath}" );
                foreach ( string directory in randomDirectoryNames.Reverse() )
                {
                    await sut.ChangeWorkingDirectoryAsync( "../" );
                    await sut.DeleteDirectoryAsync( directory );
                }
            }
        }
    }
}