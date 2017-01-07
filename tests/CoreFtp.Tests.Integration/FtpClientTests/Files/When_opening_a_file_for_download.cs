namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Infrastructure;
    using Xunit;
    using Xunit.Abstractions;

    public class When_opening_a_file_for_download : TestBase
    {
        public When_opening_a_file_for_download( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_present_read_stream_and_deliver_file( FtpEncryption encryption )
        {
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            var tempFile = ResourceHelpers.GetTempFileInfo();
            tempFile.Length.Should().Be( 0 );

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
                await sut.CreateTestResourceWithNameAsync( "test.png", randomFileName );
                using ( var ftpReadStream = await sut.OpenFileReadStreamAsync( randomFileName ) )
                {
                    using ( var fileWriteStream = tempFile.OpenWrite() )
                    {
                        await ftpReadStream.CopyToAsync( fileWriteStream );
                    }
                }

                await sut.DeleteFileAsync( randomFileName );
            }

            tempFile.Exists.Should().BeTrue();
            tempFile.Refresh();
            tempFile.Length.Should().NotBe( 0 );
            tempFile.Delete();
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_throw_exception_when_file_does_not_exist( FtpEncryption encryption )
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
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.OpenFileReadStreamAsync( $"DOES_NOT_EXIST_{Guid.NewGuid()}.png" ) );
            }
        }
    }
}