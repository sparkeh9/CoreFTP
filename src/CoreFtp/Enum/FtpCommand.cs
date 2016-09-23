namespace CoreFtp.Enum
{
    using Attributes;

    public enum FtpCommand
    {
        [ FtpCommandValue( "USER" ) ] USER,
        [ FtpCommandValue( "PASS" ) ] PASS,
        [ FtpCommandValue( "QUIT" ) ] QUIT,
        [ FtpCommandValue( "EPSV" ) ] EPSV,
        [ FtpCommandValue( "PASV" ) ] PASV,
        [ FtpCommandValue( "CWD" ) ] CWD,
        [ FtpCommandValue( "PWD" ) ] PWD,
        [ FtpCommandValue( "CLNT" ) ] CLNT,
        [ FtpCommandValue( "NLST" ) ] NLST,
        [ FtpCommandValue( "LIST" ) ] LIST,
        [ FtpCommandValue( "MLSD" ) ] MLSD,
        [ FtpCommandValue( "RETR" ) ] RETR,
        [ FtpCommandValue( "STOR" ) ] STOR,
        [ FtpCommandValue( "DELE" ) ] DELE,
        [ FtpCommandValue( "MKD" ) ] MKD,
        [ FtpCommandValue( "RMD" ) ] RMD,
        [ FtpCommandValue( "RNFR" ) ] RNFR,
        [ FtpCommandValue( "RNTO" ) ] RNTO,
        [ FtpCommandValue( "SIZE" ) ] SIZE
    }
}