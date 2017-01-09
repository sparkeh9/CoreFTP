namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Microsoft.Extensions.Logging;
    using Xunit;
    using Xunit.Abstractions;

    public class When_providing_a_base_directory : TestBase
    {
        public When_providing_a_base_directory( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_be_in_base_directory_when_logging_in( FtpEncryption encryption )
        {
            string randomDirectoryName = $"{Guid.NewGuid()}";

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
                Logger?.LogDebug( "----------Logging In first time----------" );
                await sut.LoginAsync();
                Logger?.LogDebug( "----------Creating Directory for use in basepath----------" );
                await sut.CreateDirectoryAsync( randomDirectoryName );
            }

            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = encryption == FtpEncryption.Implicit
                    ? 990
                    : Program.FtpConfiguration.Port,
                EncryptionType = encryption,
                IgnoreCertificateErrors = true,
                BaseDirectory = randomDirectoryName
            } ) )
            {
                sut.Logger = Logger;
                Logger?.LogDebug( "----------Logging In second time----------" );
                await sut.LoginAsync();
                sut.WorkingDirectory.Should().Be( $"/{randomDirectoryName}" );

                Logger?.LogDebug( "----------Deleting directory----------" );
                await sut.DeleteDirectoryAsync( $"/{randomDirectoryName}" );
            }
        }
    }
}