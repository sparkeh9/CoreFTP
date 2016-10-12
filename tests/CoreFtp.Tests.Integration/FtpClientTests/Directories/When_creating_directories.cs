namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;
    using System.Linq;
    using Infrastructure;

    public class When_creating_directories
    {
        [ Fact ]
        public async Task Should_create_a_directory()
        {
            string randomDirectoryName = Guid.NewGuid().ToString();
            ReadOnlyCollection<FtpNodeInformation> directories;

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
            {
                await sut.LoginAsync();
                await sut.CreateDirectoryAsync( randomDirectoryName );
                directories = await sut.ListDirectoriesAsync();
                await sut.DeleteDirectoryAsync( randomDirectoryName );
                await sut.LogOutAsync();
            }

            directories.Any( x => x.Name == randomDirectoryName ).Should().BeTrue();
        }
    }
}