namespace CoreFtp.Components.DirectoryListing
{
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using Infrastructure;

    internal interface IDirectoryListProvider
    {
        Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync();
        Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync();
    }
}