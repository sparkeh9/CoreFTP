namespace CoreFtp.Tests.Integration.FtpClientTests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Test to verify the fix for issue: "Can not open datastream with EncryptionType == FtpEncryption.Explicit"
    /// https://github.com/sparkeh9/CoreFTP/issues
    /// 
    /// The issue was that when trying to open a datastream with Explicit encryption,
    /// it would fail with: "Authentication failed because the remote party has closed the transport stream"
    /// 
    /// Root cause: ActivateEncryptionAsync() was being called prematurely during ConnectStreamAsync
    /// for data connections before the server was ready for SSL/TLS handshake.
    /// </summary>
    public class When_opening_datastream_with_explicit_encryption : TestBase
    {
        public When_opening_datastream_with_explicit_encryption( ITestOutputHelper outputHelper ) : base( outputHelper ) { }

        [Fact]
        public async Task Should_successfully_open_data_stream_for_download_without_ssl_authentication_error()
        {
            // Arrange
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            var tempFile = ResourceHelpers.GetTempFileInfo();

            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port,
                EncryptionType = FtpEncryption.Explicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                // Act - This should not throw "Authentication failed because the remote party has closed the transport stream"
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

            // Assert
            tempFile.Exists.Should().BeTrue();
            tempFile.Refresh();
            tempFile.Length.Should().BeGreaterThan( 0, "the downloaded file should contain data" );
            tempFile.Delete();
        }

        [Fact]
        public async Task Should_successfully_open_data_stream_for_upload_without_ssl_authentication_error()
        {
            // Arrange
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            var resourceFile = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port,
                EncryptionType = FtpEncryption.Explicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;

                // Act - This should not throw "Authentication failed because the remote party has closed the transport stream"
                await sut.LoginAsync();

                using ( var writeStream = await sut.OpenFileWriteStreamAsync( randomFileName ) )
                {
                    using ( var fileReadStream = resourceFile.OpenRead() )
                    {
                        await fileReadStream.CopyToAsync( writeStream );
                    }
                }

                // Assert
                var files = await sut.ListFilesAsync();
                files.Should().Contain( f => f.Name == randomFileName, "the uploaded file should exist on the server" );

                await sut.DeleteFileAsync( randomFileName );
            }
        }

        [Fact]
        public async Task Should_correctly_encrypt_data_channel_when_control_channel_is_encrypted()
        {
            // Arrange
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            var tempFile = ResourceHelpers.GetTempFileInfo();

            using ( var sut = new FtpClient( new FtpClientConfiguration
            {
                Host = Program.FtpConfiguration.Host,
                Username = Program.FtpConfiguration.Username,
                Password = Program.FtpConfiguration.Password,
                Port = Program.FtpConfiguration.Port,
                EncryptionType = FtpEncryption.Explicit,
                IgnoreCertificateErrors = true
            } ) )
            {
                sut.Logger = Logger;
                await sut.LoginAsync();

                // Assert - Control connection should be encrypted
                sut.IsEncrypted.Should().BeTrue( "control connection should be encrypted with Explicit encryption" );

                // Act - Open data stream and transfer data
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

            // Assert - Data should have been transferred successfully
            tempFile.Exists.Should().BeTrue();
            tempFile.Refresh();
            tempFile.Length.Should().BeGreaterThan( 0, "data transfer should succeed with encrypted data channel" );
            tempFile.Delete();
        }
    }
}
