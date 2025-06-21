using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebPackageViewer.Utilities
{
    internal class FileUtils
    {
        public static string GetShortPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // allow for extended path syntax
            bool addExtended = false;
            if (path.Length > 240 && !path.StartsWith(@"\\?\"))
            {
                path = @"\\?\" + path;
                addExtended = true;
            }

            var shortPath = new StringBuilder(1024);
            int res = GetShortPathName(path, shortPath, 1024);
            if (res < 1)
                return null;

            path = shortPath.ToString();

            if (addExtended)
                path = path.Substring(4);  // strip off \\?\

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
