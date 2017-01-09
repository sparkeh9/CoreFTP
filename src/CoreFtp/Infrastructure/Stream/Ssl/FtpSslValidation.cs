namespace CoreFtp.Infrastructure.Stream.Ssl
{
    using Stream;

    public delegate void FtpSslValidation( FtpSocketStream control, FtpSslValidationEventArgs e );
}