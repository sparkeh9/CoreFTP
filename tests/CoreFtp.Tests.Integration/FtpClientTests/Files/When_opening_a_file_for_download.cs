namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Xunit;

    public class When_opening_a_file_for_download
    {
        [ Fact ]
        public async Task Should_present_read_stream_and_deliver_file()
        {
            var tempFile = ResourceHelpers.GetTempFileInfo();
            tempFile.Length.Should().Be( 0 );

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                using ( var ftpReadStream = await sut.OpenFileReadStreamAsync( "test.png" ) )
                {
                    using ( var fileWriteStream = tempFile.OpenWrite() )
                    {
                        await ftpReadStream.CopyToAsync( fileWriteStream );
                    }
                }
            }

            tempFile.Exists.Should().BeTrue();
            tempFile.Refresh();
            tempFile.Length.Should().NotBe( 0 );
            tempFile.Delete();
        }

        [ Fact ]
        public async Task Should_throw_exception_when_file_does_not_exist()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.OpenFileReadStreamAsync( $"DOES_NOT_EXIST_{Guid.NewGuid()}.png" ) );
            }
        }
    }
}