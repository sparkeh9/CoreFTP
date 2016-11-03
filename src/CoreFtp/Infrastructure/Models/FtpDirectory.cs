namespace CoreFtp.Infrastructure.Models
{
    using System.Collections.Generic;

    public struct FtpDirectory
    {
        public int Level { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> Files { get; set; }
    }
}