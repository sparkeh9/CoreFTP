namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class When_deleting_folders
    {
        [ Fact ]
        public async Task Should_throw_exception_when_folder_nonexistent()
        {
            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                string randomDirectoryName = Guid.NewGuid().ToString();
                await sut.LoginAsync();
                await Assert.ThrowsAsync<FtpException>( () => sut.DeleteDirectoryAsync( randomDirectoryName ) );
                await sut.LogOutAsync();
            }
        }
    }
}