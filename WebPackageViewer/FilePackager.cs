using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using WebPackageViewer.Utilities;

namespace WebPackageViewer
{

    /// <summary>
    /// A file packager that can package a folder as a Zip file and 
    /// package into single file Exe and back into the 
    /// separate EXE and data parts.
    /// 
    /// It uses Native Resources that are embedded directly into the 
    /// WebPackager exe and extracted from it.
    /// </summary>
    public class FilePackager
    {
    
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

            File.Copy(exeFilename, packageFilename, true);


            // updates WEBSITE_DATA native resource with the zipped up Web site data 
            // in byte form. 
            NativeResourceHelper.UpdateResource(packageFilename, dataFilename, "WEBSITE_DATA");
          

            if (!string.IsNullOrEmpty(App.CommandLine.SignCommand))
            {
                var cmd = App.CommandLine.SignCommand.Replace("%1", "\"" + packageFilename + "\"");
                try
                {
                    ExecuteCommandLine(cmd, Path.GetDirectoryName(packageFilename), useShellExecute: false);
                }
                catch
                {
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


            var exeFile = Path.Combine(outputPath, "WebPackageViewer.exe");


            
            if (File.Exists(exeFile))
            {
                File.Delete(exeFile);                
            }   
        
            File.Copy( Assembly.GetExecutingAssembly().Location, exeFile, true );
                        
            var packageFile = Path.Combine(outputPath, "Packaged.zip");
            var siteBytes = NativeResourceHelper.ReadResource("WEBSITE_DATA");            
           
            File.WriteAllBytes(packageFile, siteBytes);
            
            if (unZip)
            {                
                if (!UnZipPackageInplace(packageFile, outputPath)) 
                {
                    return false;
                }
                //try { File.Delete(packageFile); } catch { /* ignore */ }
            }

            if (App.CommandLine.RemoveResources)
                NativeResourceHelper.UpdateResource(exeFile, "CLEAR", "WEBSITE_DATA");



            return true;
        }


        public bool UnZipPackageInplace(string packageFilename, string outputPath = null)
        {
            if(string.IsNullOrEmpty(outputPath))
                outputPath = Path.GetDirectoryName(packageFilename);
            
            try
            {
                ExtractZipFileToFolder(packageFilename, outputPath);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(outputPath))
                {
                    try { Directory.Delete(outputPath, true); } catch { /* ignore */ }
                }   
                SetError(ex, true);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Extracts a zip file to a folder. 
        /// </summary>
        /// <param name="zipFile">Zipfile to extract</param>
        /// <param name="outputFolder">Folder to extract to - supports long path names</param>
        /// <returns></returns>
        public bool ExtractZipFileToFolder(string zipFile, string outputFolder = null)
        {
            if (string.IsNullOrEmpty(outputFolder))
            {
                SetError("Output folder is not specified.");
                return false;
            }
                        
            zipFile = FileUtils.GetWindowsLongFilename(zipFile);
            using (var archive =  ZipFile.OpenRead(zipFile))
            {
                foreach (var entry in archive.Entries)
                {
                    string targetPath = Path.Combine(outputFolder, entry.FullName);
                    targetPath = targetPath.Replace('/', '\\'); // Ensure Windows path separators

                    targetPath = Path.GetFullPath(targetPath);

                    var fullPath = FileUtils.GetWindowsLongFilename(targetPath);            
                    if (entry.FullName.EndsWith("/"))
                        Directory.CreateDirectory(fullPath);
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));              
                        entry.ExtractToFile(fullPath, overwrite: true);
                    }
                }
            }

            return true;
        }


        static void ExecuteCommandLine(string fullCommandLine,
            string workingFolder = null,
            int waitForExitMs = 0,
            string verb = "OPEN",
            ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal,
            bool useShellExecute = true)
        {
            string executable = fullCommandLine;
            string args = null;

            if (executable.StartsWith("\""))
            {
                int at = executable.IndexOf("\" ");
                if (at > 0)
                {
                    // take the args as provided
                    args = executable.Substring(at + 1);
                    // plain executable
                    executable = executable.Substring(0, at).Trim(' ', '\"');
                }
            }
            else if (executable.StartsWith("\'"))
            {
                int at = executable.IndexOf("\' ");
                if (at > 0)
                {
                    // take the args as provided
                    args = executable.Substring(at + 1);
                    // plain executable
                    executable = executable.Substring(0, at).Trim(' ', '\'');
                }
            }
            else
            {
                int at = executable.IndexOf(" ");
                if (at > 0)
                {

                    if (executable.Length > at + 1)
                        args = executable.Substring(at + 1).Trim();
                    executable = executable.Substring(0, at);
                }
            }
            var pi = new ProcessStartInfo
            {
                Verb = verb,
                WindowStyle = windowStyle,
                FileName = executable,
                WorkingDirectory = workingFolder,
                Arguments = args,
                UseShellExecute = true
            };

            Process p;
            using (p = Process.Start(pi))
            {
                if (waitForExitMs > 0)
                {
                    if (!p.WaitForExit(waitForExitMs))
                        throw new TimeoutException("Process failed to complete in time.");
                }
            }
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
