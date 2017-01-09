namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Enum;
    using FluentAssertions;
    using Helpers;
    using Microsoft.Extensions.Logging;
    using Xunit;
    using Xunit.Abstractions;

    public class When_deleting_a_file : TestBase
    {
        public When_deleting_a_file( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_delete_file( FtpEncryption encryption )
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
                var fileinfo = ResourceHelpers.GetResourceFileInfo( "penguin.jpg" );

                Logger.LogDebug( "Writing the file" );
                using ( var writeStream = await sut.OpenFileWriteStreamAsync( randomFileName ) )
                {
                    var fileReadStream = fileinfo.OpenRead();
                    await fileReadStream.CopyToAsync( writeStream );
                }

                Logger.LogDebug( "Listing the directory" );
                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeTrue();

                Logger.LogDebug( "Deleting the file" );
                await sut.DeleteFileAsync( randomFileName );

                Logger.LogDebug( "Listing the firector" );
                ( await sut.ListFilesAsync() ).Any( x => x.Name == randomFileName ).Should().BeFalse();
            }
        }
    }
}