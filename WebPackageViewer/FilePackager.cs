using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using WebPackageViewer.Utilities;

namespace WebPackageViewer
{

    /// <summary>
    /// A file packager that can package a folder as a Zip file and 
    /// package into single file Exe and back into the 
    /// separate EXE and data parts.
    /// </summary>
    public class FilePackager
    {
        /// <summary>
        /// String that separates the EXE binary and the data attached.
        /// </summary>
        public string SepararatorString = "--- DATASEPARATOR ---";

        /// <summary>
        /// Separator as a byte array
        /// </summary>
        public byte[] SeparatorBytes => Encoding.UTF8.GetBytes(SepararatorString ?? string.Empty);


        /// <summary>
        /// Packages a file by combining the package EXE file and the datafile.
        /// 
        /// Appends the data to the EXE file with a separator - the original EXE
        /// still runs.
        /// </summary>
        /// <param name="packageFilename"></param>
        /// <param name="exeFilename"></param>
        /// <param name="dataFilename"></param>
        /// <returns></returns>
        public bool PackageFile(string packageFilename, string exeFilename, string dataFilename)
        {
            if (!File.Exists(exeFilename))
            {
                SetError(exeFilename + " doesn't exist.");
                return false;
            }
            if (!File.Exists(dataFilename))
            {
                SetError(dataFilename + " doesn't exist.");
                return false;
            }
            
            if (File.Exists(packageFilename))           
                File.Delete(packageFilename);

            using (var outFs = new FileStream(packageFilename, FileMode.Create, FileAccess.Write))
            {
                using (var fs = new FileStream(exeFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.CopyTo(outFs);
                    outFs.Flush();
                    outFs.Write(SeparatorBytes, 0, SeparatorBytes.Length);
                };
                using (var fs = new FileStream(dataFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.CopyTo(outFs);
                }
            }

            return true;
        }

        /// <summary>
        /// Zips up a folder and returns the output file name.
        /// </summary>
        /// <param name="zipFolder">Folder to zip up</param>
        /// <param name="outputZipFilename">Zip file to create</param>
        /// <returns>the output zip file name or null if it fails</returns>        
        public string ZipFolder(string zipFolder, string outputZipFilename = null)
        {
            if (string.IsNullOrEmpty(zipFolder))
            {
                SetError("Zip folder does not exist.");
                return null;
            }
            zipFolder = Path.GetFullPath(zipFolder);

            if (!Directory.Exists(zipFolder))
            {
                SetError("Zip folder does not exist.");
                return null;
            }

            if (string.IsNullOrEmpty(outputZipFilename))
            {
                outputZipFilename = Path.Combine(Path.GetTempPath(), $"WebView-Package-{StringUtils.GenerateUniqueId(6)}.zip");
            }
            else
            {
                outputZipFilename = Path.GetFullPath(outputZipFilename);
            }

            try
            {
                ZipFile.CreateFromDirectory(zipFolder, outputZipFilename, CompressionLevel.Optimal, false);
            }
            catch (Exception ex)
            {
                SetError(ex, true);
                return null;
            }

            return outputZipFilename;
        }


        public bool UnpackageFile(string packageFilename,
            string outputPath = null,
            bool unZip = true,
            bool noDirectoryCheck = false)
        {   
            if (!File.Exists(packageFilename))
            {
                SetError("Package file does not exist.");
                return false;
            }


            if (Directory.Exists(outputPath))
            {
                SetError("Output path exists already. Make sure you use a unique folder name or clear the folder to avoid overwriting files.");
                return false;
            }
            
            Directory.CreateDirectory(outputPath);


            var offset = FindMarkerOffset(packageFilename, SeparatorBytes);
            if (offset < 0)
            {
                SetError("File is missing Packaged Html Data.");
                return false;
            }

            var exeFile = Path.Combine(outputPath, "WebPackageViewer.exe");
            using (var exeFs = new FileStream(exeFile, FileMode.Create, FileAccess.Write, FileShare.Write))
            using (var fs = new FileStream(packageFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                CopyToBytes(fs, exeFs, offset - SeparatorBytes.Length);
                fs.Flush();
            }

            var packageFile = Path.Combine(outputPath, "Packaged.zip");
            using (var packageFs = new FileStream(packageFile, FileMode.Create, FileAccess.Write, FileShare.Write))
            using (var fs = new FileStream(packageFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(offset, SeekOrigin.Begin);
                fs.CopyTo(packageFs);
            }

            if (unZip)
            {
               UnZipPackageInplace(packageFile, outputPath);
               File.Delete(packageFile);
            }

            return true;
        }


        public bool UnZipPackageInplace(string packageFilename, string outputPath = null)
        {
            if(string.IsNullOrEmpty(outputPath))
                outputPath = Path.GetDirectoryName(packageFilename);

            try
            {
                ZipFile.ExtractToDirectory(packageFilename, outputPath);
            }
            catch (Exception ex)
            {
                SetError(ex, true);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Like CopyTo but allows you to specify the number of bytes to copy and a buffer size.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="bytesToCopy"></param>
        /// <param name="bufferSize"></param>
        void CopyToBytes(Stream source, Stream destination, long bytesToCopy, int bufferSize = 81920)
        {
            byte[] buffer = new byte[Math.Min(bufferSize, (int)bytesToCopy)];
            int bytesRead;
            long totalCopied = 0;

            while (totalCopied < bytesToCopy &&
                   (bytesRead = source.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesToCopy - totalCopied))) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                totalCopied += bytesRead;
            }
        }

        /// <summary>
        /// Find the offset of a marker in a file.
        /// </summary>
        /// <param name="exePath"></param>
        /// <param name="marker"></param>
        /// <returns></returns>
        public long FindMarkerOffset(string exePath, byte[] marker)
        {            
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize + marker.Length - 1];

            using (var fs = new FileStream(exePath, FileMode.Open, FileAccess.Read))
            {
                long position = 0;
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i <= bytesRead - marker.Length; i++)
                    {
                        bool match = true;
                        for (int j = 0; j < marker.Length; j++)
                        {
                            if (buffer[i + j] != marker[j])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (match)
                            return position + i + marker.Length;
                    }

                    // Slide window back for overlap (in case marker splits across reads)
                    if (bytesRead < marker.Length)
                        break;
                    position += bytesRead - marker.Length + 1;
                    fs.Seek(position, SeekOrigin.Begin);
                }
            }

            return -1;
        }

        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                ErrorMessage = string.Empty;
                return;
            }
            ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
            {
                ErrorMessage = string.Empty;
            }
            else
            {
                Exception e = ex;
                if (checkInner)
                    e = e.GetBaseException();

                ErrorMessage = e.Message;
            }
        }
    }
}
