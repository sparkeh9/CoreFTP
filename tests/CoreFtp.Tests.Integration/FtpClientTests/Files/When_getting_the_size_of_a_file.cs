namespace CoreFtp.Tests.Integration.FtpClientTests.Files
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Helpers;
    using Infrastructure;
    using Xunit;

    public class When_getting_the_size_of_a_file
    {
        public When_getting_the_size_of_a_file()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_give_size()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
                                             } ) )
            {
                string randomFilename = $"{Guid.NewGuid()}.jpg";
                await sut.LoginAsync();

                await sut.CreateTestResourceWithNameAsync( "test.png", randomFilename );
                long size = await sut.GetFileSizeAsync( randomFilename );

                size.Should().Be( 34427 );

                await sut.DeleteFileAsync( randomFilename );
            }
        }

        [ Fact ]
        public async Task Shouldthrow_exception_when_file_nonexistent()
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

                await Assert.ThrowsAsync<FtpException>( () => sut.GetFileSizeAsync( $"{Guid.NewGuid()}.png" ) );
            }
        }
    }
}