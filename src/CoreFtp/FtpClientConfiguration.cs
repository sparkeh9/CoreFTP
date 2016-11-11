namespace CoreFtp
{
    using Enum;

    public class FtpClientConfiguration
    {
        public int TimeoutSeconds { get; set; } = 120;
        public int BufferSize { get; set; } = 512;
        public int Port { get; set; } = 21;
        public string Host { get; set; }
        public IpVersion IpVersion { get; set; } = IpVersion.IpV4;
        public string Username { get; set; }
        public string Password { get; set; }
        public string BaseDirectory { get; set; } = "/";
        public FtpTransferMode Mode { get; set; } = FtpTransferMode.Binary;
        public char ModeSecondType { get; set; } = '\0';
    }
}