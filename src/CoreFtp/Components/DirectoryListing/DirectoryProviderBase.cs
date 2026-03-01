namespace CoreFtp.Components.DirectoryListing
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Infrastructure;
    using Microsoft.Extensions.Logging;

    internal abstract class DirectoryProviderBase : IDirectoryProvider
    {
        protected FtpClient ftpClient;
        protected FtpClientConfiguration configuration;
        protected ILogger logger;
        protected Stream stream;

        protected async Task<List<string>> RetrieveDirectoryListingAsync()
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(stream, ftpClient.ControlStream.Encoding))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    logger?.LogDebug(line);
                    lines.Add(line);
                }
            }

            return lines;
        }

        public virtual Task<ReadOnlyCollection<FtpNodeInformation>> ListAllAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task<ReadOnlyCollection<FtpNodeInformation>> ListFilesAsync()
        {
            throw new NotImplementedException();
        }

        public virtual Task<ReadOnlyCollection<FtpNodeInformation>> ListDirectoriesAsync()
        {
            throw new NotImplementedException();
        }
    }
}