using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebPackageViewer.CommandLine
{
    public class WebPackageViewerCommandLine : CommandLineParser
    {

        public string SourcePath { get; set; }

        public string OutputPath { get; set; }

        public string PackageFile { get; set; }
        public string ExeFile { get; set; }

        /// <summary>
        /// You can specify a Zip file that is packaged with the 
        /// Exe. If this is specified ZipFolder is ignored.
        /// </summary>
        public string ZipFilename { get; set; }
        
        /// <summary>
        /// A folder to zip up and attach to the Exe file.
        /// </summary>
        public string ZipFolder { get; set; }

        
        public string VirtualPath { get; set; }


        public string InitialUrl { get; set; } 

        /// <summary>
        /// Set if Parse didn't process the command
        /// </summary>
        public bool Unhandled { get; set; }

        public override void Parse()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch { }

            SourcePath = ParseStringParameterSwitch("--source", null);
            OutputPath = ParseStringParameterSwitch("--output", null);            
            ExeFile = ParseStringParameterSwitch("--exe", null);
            if (string.IsNullOrEmpty(ExeFile)) 
                ExeFile = typeof(App).Assembly.Location;

            ZipFilename = ParseStringParameterSwitch("--zipfile", null);
            if (string.IsNullOrEmpty(ZipFilename))
            {
                // Zipfolder is not used if a Zip file is provided
                ZipFolder = ParseStringParameterSwitch("--zipfolder", null);
            }
            PackageFile = ParseStringParameterSwitch("--package", "packageFile");
            VirtualPath = ParseStringParameterSwitch("--virtual", null);
            InitialUrl = ParseStringParameterSwitch("--initialurl", null);


            var first = FirstParameter?.ToLowerInvariant() ?? string.Empty;
            if (first.StartsWith("-"))
                first = string.Empty; // ignore any switches - only folder or commands are valid

            if (first == "package")
            {
                ConsoleHelper.WriteWrappedHeader("West Wind Web Package Viewer");
                Console.WriteLine("📦 Packaging zip file...");

                string zipFile = ZipFilename;

                var pack = new FilePackager();
                if (!string.IsNullOrEmpty(ZipFolder))
                {                                     
                    zipFile = pack.ZipFolder(ZipFolder);
                    if (zipFile == null)
                    {
                        Console.WriteLine("❌ Error creating Zip file for package: " + pack.ErrorMessage);
                        return;
                    }
                }

                if (!pack.PackageFile(Path.GetFullPath(OutputPath), 
                    Path.GetFullPath(ExeFile), 
                    Path.GetFullPath(zipFile)))
                {                    
                    Console.WriteLine("❌ Error creating package: " + pack.ErrorMessage);
                    return;
                }                
                Console.WriteLine("✅ Package has been created:");
                ColorConsole.WriteLine( OutputPath, ConsoleColor.DarkYellow);
                return;
            }
            else if(first == "unpackage")
            {
                ConsoleHelper.WriteWrappedHeader("West Wind Web Package Viewer");
                Console.WriteLine("📦 Unpackaging zip file...");

                var pack = new FilePackager();
                if (!pack.UnpackageFile(Path.GetFullPath(PackageFile), Path.GetFullPath(OutputPath), true))
                {
                    ColorConsole.Write("❌ ");
                    Console.WriteLine("Error unpackaging file: " + pack.ErrorMessage);
                    return;
                }
                Console.Write("✅ ");
                Console.WriteLine("Package has unpacked into:");
                ColorConsole.WriteLine(OutputPath, ConsoleColor.DarkYellow);

                // don't return here - we want to run the unpackaged site
            }
            else if (first == "help" || first == "?")
            {
                ConsoleHelper.WriteWrappedHeader("West Wind Web Package Viewer");                

                ColorConsole.WriteLine(@"A Web Site Viewer that can:

* Run a static Web site from a folder
  and display it in an internal WebView
* Can package a Web site into a *single file Exe*
  and run it to unpack and display the site
* Can unpackage a packaged Web site into its
  Exe and Web site files
", ConsoleColor.Gray);
                    


                ColorConsole.WriteLine("Usage: WebPackageViewer [foldername | command] [options]", ConsoleColor.Cyan);
                
                ColorConsole.WriteLine("\nFoldername:", ConsoleColor.Green);
                ColorConsole.WriteLine("Runs a web site in that location or if no folder in the current folder.\n" +
                                       "If packaged, unpacks into a tempfolder and runs the site from there.", ConsoleColor.White);

                ColorConsole.WriteLine("\nCommands:", ConsoleColor.Green);
                ColorConsole.WriteLine("  package   - Create a package from an executable and a zip file", ConsoleColor.White);
                ColorConsole.WriteLine("  unpackage - Unpackage the Exe and Website into the output folder", ConsoleColor.White);
                ColorConsole.WriteLine("  help      - Show this help message", ConsoleColor.White);

                ColorConsole.WriteLine("\nOptions:", ConsoleColor.Green);
                ColorConsole.WriteLine("  --output     - package: Output filename for the packaged exe\n" +
                                       "                 unpackage: Output folder where the Web site and Exe is unpackaged to", ConsoleColor.White);
                ColorConsole.WriteLine("  --exe        - Optional Exe file to package. If not specified source exe is used", ConsoleColor.White);
                ColorConsole.WriteLine("  --zipfile    - An existing Zip File to package (priortized over --zipfolder)", ConsoleColor.White);
                ColorConsole.WriteLine("  --zipfolder  - A folder to zip up and then package", ConsoleColor.White);
                ColorConsole.WriteLine("  --virtual    - Virtual Path when running the site (/,/docs)");
                ColorConsole.WriteLine("  --initialurl - Initial URL to load in the WebView (/index.html, /docs/index.html)", ConsoleColor.White);

                Console.WriteLine("\n");

                //ColorConsole.WriteLine("\nExamples", ConsoleColor.Green);
                

                ColorConsole.WriteLine("Configuration File", ConsoleColor.Green);
                Console.WriteLine("You can optionally provide a configuration in your Webroot Folder\n" +
                                  "that allows you to customize how the Packaged site runs:");

                var json = @"
// WebPackageViewer.config.json
{
    ""VirtualPath"": ""/docs"",
    ""InitialUrl"": ""/docs/index.html"",
    ""Window Title"": ""West Wind Web Package Viewer"",
    ""WindowSize"": ""1280x800""
}";
                ColorConsole.WriteLine(json, ConsoleColor.DarkGray);

                return;
            }

            Unhandled = true;
        }

        
    }
}
