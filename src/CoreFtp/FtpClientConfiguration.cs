namespace CoreFtp
{
    using System;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using Enum;
    using Infrastructure;

    public class FtpClientConfiguration
    {
        public int TimeoutSeconds { get; set; } = 120;
        public int? DisconnectTimeoutMilliseconds { get; set; } = 100;
        public int Port { get; set; } = Constants.FtpPort;
        public string Host { get; set; }
        public IpVersion IpVersion { get; set; } = IpVersion.IpV4;
        public FtpEncryption EncryptionType { get; set; } = FtpEncryption.None;
        public bool IgnoreCertificateErrors { get; set; } = true;
        public string Username { get; set; }
        public string Password { get; set; }
        public string BaseDirectory { get; set; } = "/";
        public FtpTransferMode Mode { get; set; } = FtpTransferMode.Binary;
        public char ModeSecondType { get; set; } = '\0';

        public bool ShouldEncrypt => EncryptionType == FtpEncryption.Explicit ||
                                     EncryptionType == FtpEncryption.Implicit &&
                                     Port == Constants.FtpsPort;

        public X509CertificateCollection ClientCertificates { get; set; } = new X509CertificateCollection();
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// Base encoding to use for the control stream. Useful for legacy servers that use Shift_JIS, GBK, etc.
        /// </summary>
        public System.Text.Encoding BaseEncoding { get; set; } = System.Text.Encoding.ASCII;

        /// <summary>
        /// Allows overriding the server certificate validation logic (e.g., verifying a specific self-signed certificate thumbprint).
        /// </summary>
        public Func<X509Certificate, X509Chain, SslPolicyErrors, bool> ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// Allows overriding the OS directory provider. Useful if the server does not report its OS in FEAT (e.g. FtpFileSystemType.Windows).
        /// </summary>
        public FtpFileSystemType? ForceFileSystem { get; set; }
    }
}
