namespace CoreFtp.Tests.Integration.FtpClientTests.Directories
{
    using System;
    using CoreFtp.Components.DirectoryListing.Parser;
    using CoreFtp.Enum;
    using Xunit;

    public class When_Encountering_Differing_Directory_Formats : TestBase
    {
        [Theory]
        [InlineData("-rwx---A--  1 user    group       392468 Nov 13 07:20 Group-2~20121112_0020BD3001000614.uhh", FtpNodeType.File, 392468, "-11-13T07:20", "Group-2~20121112_0020BD3001000614.uhh")]
        [InlineData("-rwx----S-  1 user    group         9702 Jan 23 15:23 Group-1~20130123_0196EE300000005D.uhh", FtpNodeType.File, 9702, "-01-23T15:23", "Group-1~20130123_0196EE300000005D.uhh")]
        [InlineData("-rw-rw-r--    1 1000     1000         5647 Nov 14 16:03 Furnace-1~~20080630_00208930000034A0.uhh", FtpNodeType.File, 5647, "-11-14T16:03", "Furnace-1~~20080630_00208930000034A0.uhh")]
        [InlineData("-r--r--r-- 1 ftp ftp             52 Nov 08 20:15 Batches~Batch-1~20000101_0001acxx4105000001.uhh", FtpNodeType.File, 52, "-11-08T20:15", "Batches~Batch-1~20000101_0001acxx4105000001.uhh")]
        [InlineData("drwxr-xr-x 1 ftp ftp              0 Nov 08 20:15 history", FtpNodeType.Directory, 0, "-11-08T20:15", "history")]
        [InlineData("-rwx------  1 user    group         1171 Oct  3  2011 FD8~20111003_801C5826000007EB.uhh", FtpNodeType.File, 1171, "2011-10-03", "FD8~20111003_801C5826000007EB.uhh")]
        [InlineData("-rwx------  1 user    group         1170 Oct  4  2011 C1001283~0142A82401001283.UHH", FtpNodeType.File, 1170, "2011-10-04", "C1001283~0142A82401001283.UHH")]
        [InlineData("drwxrwxr-x    3 1000     1000         4096 Aug 08 09:09 1", FtpNodeType.Directory, 4096, "-08-08T09:09", "1")]
        [InlineData("drwxrwxr-x    3 1000     1000         4096 Aug 08 09:09 ÷¿¡¥¢{$&GS", FtpNodeType.Directory, 4096, "-08-08T09:09", "÷¿¡¥¢{$&GS")]
        [InlineData("drwxrwxr-x    3 1000     1000         4096 Aug 08 09:09 密码౱㙔㥓戀氀欀", FtpNodeType.Directory, 4096, "-08-08T09:09", "密码౱㙔㥓戀氀欀")]
        [InlineData("drwxr-xr-x 1 ftp ftp              0 Oct 08  2008 347Arch", FtpNodeType.Directory, 0, "2008-10-08", "347Arch")]
        // Listings from an IIS FTP server (note the single digit dates)
        [InlineData("-r-xr-xr-x   1 owner    group           14271 May  3  2:24 02001318~01488A2402001318.UHH", FtpNodeType.File, 14271, "-05-03T02:24", "02001318~01488A2402001318.UHH")]
        [InlineData("dr-xr-xr-x   1 owner    group               0 Oct 29  2013 Eycon", FtpNodeType.Directory, 0, "2013-10-29", "Eycon")]
        [InlineData("dr-xr-xr-x   1 owner    group               0 Jun  4  9:21 EYCON_40", FtpNodeType.Directory, 0, "-06-04T09:21", "EYCON_40")]
        // Baby FTP (http://www.pablosoftwaresolutions.com/html/baby_ftp_server.html)
        [InlineData("-rwx------ 1 user group         400366 Jan 04 2011  DoncastersNo-417~816~20110103_00F1ED300000087D.uhh", FtpNodeType.File, 400366, "2011-01-04T00:00", "DoncastersNo-417~816~20110103_00F1ED300000087D.uhh")]
        public void Should_Accept(string listing, FtpNodeType nodeType, int size, string when, string filename)
        {
            if (when[0] == '-')
            {
                when = DateTime.Now.Year.ToString() + when;
            }

            var dateTime = DateTime.Parse(when);
            var parser = new UnixDirectoryParser(null);
            var result = parser.Parse(listing);

            Assert.Equal(nodeType, result.NodeType);
            Assert.Equal(filename, result.Name);
            Assert.Equal(size, result.Size);
            Assert.Equal(dateTime, result.DateModified);
        }
    }
}
