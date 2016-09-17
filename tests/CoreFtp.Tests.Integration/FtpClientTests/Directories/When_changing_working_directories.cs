namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    public class When_changing_working_directories
    {
        [ Fact ]
        public async Task Should_fail_when_changing_to_a_nonexistent_directory()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_fail_when_changing_to_a_nonexistent_directory ) );
                await Assert.ThrowsAsync<FtpException>( () => sut.ChangeWorkingDirectoryAsync( Guid.NewGuid().ToString() ) );
            }
        }

        [ Fact ]
        public async Task Should_change_to_directory_when_exists()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_change_to_directory_when_exists ) );
                await sut.ChangeWorkingDirectoryAsync( "test1" );
                sut.WorkingDirectory.Should().Be( "/test1" );
            }
        }

        [ Fact ]
        public async Task Should_change_to_deep_directory_when_exists()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.SetClientName( nameof( Should_change_to_deep_directory_when_exists ) );
                await sut.ChangeWorkingDirectoryAsync( "test1/test1_1" );
                sut.WorkingDirectory.Should().Be( "/test1/test1_1" );
            }
        }
    }
}