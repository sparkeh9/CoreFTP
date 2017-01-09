namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AsyncFriendlyStackTrace;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Infrastructure;
    using Xunit;
    using Xunit.Abstractions;

    public class When_deleting_directories : TestBase
    {
        public When_deleting_directories( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_throw_exception_when_folder_nonexistent( FtpEncryption encryption )
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

                string randomDirectoryName = Guid.NewGuid().ToString();
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.DeleteDirectoryAsync( randomDirectoryName ) );
                await sut.LogOutAsync();
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_delete_directory_when_exists( FtpEncryption encryption )
        {
            string randomDirectoryName = Guid.NewGuid().ToString();

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

                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeTrue();
                await sut.DeleteDirectoryAsync( randomDirectoryName );
                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeFalse();
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_recursively_delete_folder( FtpEncryption encryption )
        {
            string randomDirectoryName = Guid.NewGuid().ToString();

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
                await sut.LoginAsync();

                await sut.CreateTestResourceWithNameAsync( "penguin.jpg", $"{randomDirectoryName}/1/penguin.jpg" );

                await sut.CreateDirectoryAsync( $"{randomDirectoryName}/1/1/1" );
                await sut.CreateDirectoryAsync( $"{randomDirectoryName}/1/1/2" );
                await sut.CreateDirectoryAsync( $"{randomDirectoryName}/1/2/2" );
                await sut.CreateDirectoryAsync( $"{randomDirectoryName}/2/2/2" );

                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeTrue();
                try
                {
                    await sut.DeleteDirectoryAsync( randomDirectoryName );
                }
                catch ( Exception e )
                {
                    throw new Exception( e.ToAsyncString() );
                }

                ( await sut.ListDirectoriesAsync() ).Any( x => x.Name == randomDirectoryName ).Should().BeFalse();
            }
        }
    }
}