namespace CoreFtp.Components.DirectoryListing
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Infrastructure;

    internal interface IDirectoryProvider
    {
        /// <summary>
        /// Lists all nodes in the current working directory
        /// </summary>
        /// <returns></returns>
        Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync();

        /// <summary>
        /// Lists all files in the current working directory
        /// </summary>
        /// <returns></returns>
        Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync();

        /// <summary>
        /// Lists directories beneath the current working directory
        /// </summary>
        /// <returns></returns>
        Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync();

        /// <summary>
        /// Streams all nodes in the current working directory as they are parsed
        /// </summary>
        IAsyncEnumerable<FtpNodeInformation> ListAllEnumerableAsync( CancellationToken cancellationToken = default );

        /// <summary>
        /// Streams all files in the current working directory as they are parsed
        /// </summary>
        IAsyncEnumerable<FtpNodeInformation> ListFilesEnumerableAsync( CancellationToken cancellationToken = default );

        /// <summary>
        /// Streams all directories in the current working directory as they are parsed
        /// </summary>
        IAsyncEnumerable<FtpNodeInformation> ListDirectoriesEnumerableAsync( CancellationToken cancellationToken = default );
    }
}