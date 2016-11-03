namespace CoreFtp.Components.DirectoryListing
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Infrastructure;

    internal interface IDirectoryProvider
    {
        Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync();
        Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync();
        Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync();
    }
}