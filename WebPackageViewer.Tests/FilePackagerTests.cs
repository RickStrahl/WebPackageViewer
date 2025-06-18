using System.IO;

namespace WebPackageViewer.Tests
{
    [TestClass]
    public sealed class FilePackagerTests
    {
        [TestMethod]
        public void PackageTest()
        {
            var pack = new FilePackager();

            var exeFile = @"C:\Users\rstrahl\Documents\Documentation Monster\WebSurge\wwwroot\WebPackageViewer.exe";
            var packageFile = @"C:\Users\rstrahl\Documents\Documentation Monster\WebSurge\wwwroot\WebPackageViewer-Packaged.exe";
            var dataFile = @"C:\Users\rstrahl\Documents\Documentation Monster\WebSurge\wwwroot\PackagedSite.zip";

            Assert.IsTrue(pack.PackageFile(packageFile, exeFile, dataFile), pack.ErrorMessage);

        }

        [TestMethod]
        public void UnPackageTest()
        {
            var pack = new FilePackager();


            var packageFile = @"C:\Users\rstrahl\Documents\Documentation Monster\WebSurge\wwwroot\WebPackageViewer-Packaged.exe";
            var unpackFolder = @"C:\Users\rstrahl\Documents\Documentation Monster\WebSurge\wwwroot\Unpacked";
            
            if (Directory.Exists(unpackFolder))
                Directory.Delete(unpackFolder, true);

            Assert.IsTrue(pack.UnpackageFile(packageFile,unpackFolder), pack.ErrorMessage);

        }
    }
}
