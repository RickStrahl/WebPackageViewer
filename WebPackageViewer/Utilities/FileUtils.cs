using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebPackageViewer.Utilities
{
    internal class FileUtils
    {

        /// <summary>
        /// Retrieves a long filename for a given path using
        /// Extended Windows path syntax.        
        /// 
        /// Fully qualified paths:
        /// \\?\C:\path\to\file.txt
        /// UNC Paths:
        /// \\?\UNC\server\share\path\to\file.txt
        /// </summary>
        /// <remarks>       
        /// Paths are resolved using GetFullPath() so preferably
        /// you should pass in a fully qualified path for your 
        /// application to ensure that path is resolved correctly.
        /// </remarks>
        /// <param name="path">Existing path</param>
        /// <returns>Long path</returns>
        public static string GetWindowsLongFilename(string path)
        {
            if(string.IsNullOrEmpty(path))
                return path;

            string fullPath = System.IO.Path.GetFullPath(path);

            if (fullPath.Length < 260)
                return fullPath; // No need to convert

            // Fully qualified path
            if (fullPath.Length > 1 && fullPath[1] == ':') 
                fullPath = @"\\?\" + fullPath;
            // UNC Path
            else if (fullPath.Length > 2 && fullPath.StartsWith(@"\\"))
                fullPath = @"\\?\UNC\" + fullPath.Substring(2);

            return fullPath;
        }


        public static string GetShortPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

          

            var shortPath = new StringBuilder(1024);         
            int res = GetShortPathName(path, shortPath, 1024);
            if (res < 1)
                return null;

            path = shortPath.ToString();

            return path;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder shortPath,
            int shortPathLength
        );
    }
}
