namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class When_opening_file_write_stream : TestBase
    {
        public When_opening_file_write_stream( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_not_hang( FtpEncryption encryption )
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
                await sut.LoginAsync();
                var stream = await OpenWriteAsync( sut, "/", randomFileName );
                stream.Dispose();
            }
        }

        public async Task<Stream> OpenWriteAsync( FtpClient ftpClient, string path, string fileName )
        {
            await ftpClient.CreateDirectoryAsync( path );
            await ftpClient.ChangeWorkingDirectoryAsync( path );
            return await ftpClient.OpenFileWriteStreamAsync( fileName );
        }
    }
}