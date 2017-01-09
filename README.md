# CoreFTP

[![NuGet](https://img.shields.io/nuget/v/CoreFtp.svg)](https://www.nuget.org/packages/CoreFtp/)
[![Build Status: Windows](https://ci.appveyor.com/api/projects/status/github/sparkeh9/coreftp?branch=master&svg=true)](https://ci.appveyor.com/api/projects/status/github/sparkeh9/coreftp?branch=master&svg=true)

CoreFTP is a simple .NET FTP library written entirely in C#, it is targeted at netstandard 1.3, meaning it will run under .NET Core (which is also where it derives its name) and the full .NET framework.
This package was inspired due to a lack of packages providing FTP functionality compiled with support for the netstandard API surface.

NuGet page is at: https://www.nuget.org/packages/CoreFtp/

### .NET Framework compatability ###
CoreFTP Supports and includes compiled binaries for:
- NetStandard 1.3 and above
- .NET Framework 4.5.2 and above


###Usage###
Usage of this small library was intended to be as simple as possible. The integration test suite provides various example of basic FTP usage.

#### Connecting to an FTP/S server ####
Connecting to FTP/s supports both Explicit and Implicit modes.
```
using ( var ftpClient = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password",
                                                 Port = 990,
                                                 EncryptionType = FtpEncryption.Implicit,
                                                 IgnoreCertificateErrors = true
                                             } ) )
{
    await ftpClient.LoginAsync();
}

```

####Downloading a file to a filestream on local disk ####

```
using ( var ftpClient = new FtpClient( new FtpClientConfiguration
                                             {
                                                 Host = "localhost",
                                                 Username = "user",
                                                 Password = "password"
                                             } ) )
{
	var tempFile = new FileInfo( "C:\\test.png" );
    await ftpClient.LoginAsync();
    using ( var ftpReadStream = await ftpClient.OpenFileReadStreamAsync( "test.png" ) )
    {
        using ( var fileWriteStream = tempFile.OpenWrite() )
        {
            await ftpReadStream.CopyToAsync( fileWriteStream );
        }
    }
}

```

#### Uploading from a local filestream to the FTP server ####

```
using ( var ftpClient = new FtpClient( new FtpClientConfiguration
                                    {
                                        Host = "localhost",
                                        Username = "user",
                                        Password = "password"
                                    } ) )
{
	var fileinfo = new FileInfo( "C:\\test.png" );
    await ftpClient.LoginAsync();    

    using ( var writeStream = await ftpClient.OpenFileWriteStreamAsync( "test.png" ) )
    {
        var fileReadStream = fileinfo.OpenRead();
        await fileReadStream.CopyToAsync( writeStream );
    }
}

```

###Integration Tests ###
Integration tests can be run against most FTP servers with passive mode enabled, credentials can be configured in appsettings.json of CoreFtp.Tests.Integration.
