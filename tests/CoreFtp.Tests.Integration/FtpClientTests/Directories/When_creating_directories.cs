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
        public When_creating_directories()
        {
            Program.Initialise();
        }

        [ Fact ]
        public async Task Should_create_a_directory()
        {
            string randomDirectoryName = Guid.NewGuid().ToString();
            ReadOnlyCollection<FtpNodeInformation> directories;

            using ( var sut = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = Program.FtpConfiguration.Host,
                                                 Username = Program.FtpConfiguration.Username,
                                                 Password = Program.FtpConfiguration.Password,
                                                 Port = Program.FtpConfiguration.Port
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