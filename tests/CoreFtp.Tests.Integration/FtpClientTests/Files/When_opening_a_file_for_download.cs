namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Infrastructure;
    using Xunit;

    public class When_opening_a_file_for_download
    {
        public When_opening_a_file_for_download()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_present_read_stream_and_deliver_file()
        {
            string randomFileName = $"{Guid.NewGuid()}.jpg";
            var tempFile = ResourceHelpers.GetTempFileInfo();
            tempFile.Length.Should().Be( 0 );

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
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

        [ Fact ]
        public async Task Should_throw_exception_when_file_does_not_exist()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.OpenFileReadStreamAsync( $"DOES_NOT_EXIST_{Guid.NewGuid()}.png" ) );
            }
        }
    }
}