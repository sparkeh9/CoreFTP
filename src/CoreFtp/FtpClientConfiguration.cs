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
        /// Note: This callback is only invoked when <see cref="IgnoreCertificateErrors"/> is set to <c>false</c>.
        /// When <see cref="IgnoreCertificateErrors"/> is <c>true</c> (the default), certificate errors are ignored and this callback is not used.
        /// </summary>
        public Func<X509Certificate, X509Chain, SslPolicyErrors, bool> ServerCertificateValidationCallback { get; set; }

        /// <summary>
        /// Allows overriding the detected server file system / directory listing format.
        /// Useful when the server does not advertise MLSD in its FEAT response (causing a fallback to LIST),
        /// and the automatic detection of the LIST output format fails (e.g. force FtpFileSystemType.Windows).
        /// </summary>
        public FtpFileSystemType? ForceFileSystem { get; set; }

        /// <summary>
        /// Specifies the FTP data connection type. Default is <see cref="FtpDataConnectionType.AutoPassive"/>
        /// which uses EPSV/PASV (client connects to server's data port).
        /// Set to <see cref="FtpDataConnectionType.Active"/> to use PORT/EPRT (server connects back to client).
        /// </summary>
        public FtpDataConnectionType DataConnectionType { get; set; } = FtpDataConnectionType.AutoPassive;

        /// <summary>
        /// The external IP address to advertise in Active mode PORT/EPRT commands.
        /// Required when the client is behind NAT and the server cannot reach the client's local IP.
        /// If null, the local IP address of the control connection socket is used.
        /// </summary>
        public string ActiveExternalIp { get; set; }
    }
}
