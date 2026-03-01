namespace CoreFtp.Enum
{
    /// <summary>
    /// Specifies the FTP data connection type used for file transfers and directory listings
    /// </summary>
    public enum FtpDataConnectionType
    {
        /// <summary>
        /// Default. Use Extended Passive (EPSV) first, fallback to Passive (PASV).
        /// Client connects to the server's data port.
        /// </summary>
        AutoPassive,

        /// <summary>
        /// Use Active mode (PORT/EPRT).
        /// Client listens on a local port and the server connects back to it.
        /// Requires the client to be reachable from the server (not behind NAT without configuration).
        /// </summary>
        Active,
    }
}
