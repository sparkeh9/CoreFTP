# CoreFTP

[![Build Status: Windows](https://ci.appveyor.com/api/projects/status/github/sparkeh9/coreftp?branch=master&svg=true)](https://ci.appveyor.com/api/projects/status/github/sparkeh9/coreftp?branch=master&svg=true)

CoreFTP is a simple .NET FTP library written entirely in C#, with no external dependencies.
NuGet page is at: https://www.nuget.org/packages/CoreFtp/

###Usage###
Usage of this small library was intended to be as simple as possible. The integration test suite provides various example of basic FTP usage.

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
		await ftpClient.CloseFileWriteStreamAsync();
    }
}

```

###Integration Tests ###

Integration tests rely on an FTP server running on localhost with passive mode enabled, with the following folder structure in place
(N.B. the image used is supplied in the integration tests resource folder)

```
.
+-- test1
|   +-- test1_1
|		+-- test.png
|   +-- test1_2
|	+-- test.png
+-- test2
+-- test.png 
```