namespace CoreFtp.Tests.Integration
{
    using Enum;

    public class FtpConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public FtpEncryption EncryptionType { get; set; } = FtpEncryption.Implicit;
    }
}