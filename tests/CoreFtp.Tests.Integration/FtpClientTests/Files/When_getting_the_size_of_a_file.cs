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

    public class When_getting_the_size_of_a_file : TestBase
    {
        public When_getting_the_size_of_a_file( ITestOutputHelper outputHelper ) : base( outputHelper ) {}

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_give_size( FtpEncryption encryption )
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
                string randomFilename = $"{Guid.NewGuid()}.jpg";
                await sut.LoginAsync();

                await sut.CreateTestResourceWithNameAsync( "test.png", randomFilename );
                long size = await sut.GetFileSizeAsync( randomFilename );

                size.Should().Be( 34427 );

                await sut.DeleteFileAsync( randomFilename );
            }
        }

        [ Theory ]
        [ InlineData( FtpEncryption.None ) ]
        [ InlineData( FtpEncryption.Explicit ) ]
        [ InlineData( FtpEncryption.Implicit ) ]
        public async Task Should_throw_exception_when_file_nonexistent( FtpEncryption encryption )
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

                await Assert.ThrowsAsync<FtpException>( () => sut.GetFileSizeAsync( $"{Guid.NewGuid()}.png" ) );
            }
        }
    }
}