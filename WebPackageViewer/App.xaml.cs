using System;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Input;
using WebPackageViewer.Utilities;
using WebPackageViewer.CommandLine;


namespace WebPackageViewer
{
    public partial class App : Application
    {
        public static string InitialStartDirectory { get; set; }

        public static string InitialUserStartedDirectory { get; set; }

        public static string TempUnpackDirectory { get; set; }

        public static WebPackageViewerCommandLine CommandLine { get; set; } = new WebPackageViewerCommandLine();

       

        protected override async void OnStartup(StartupEventArgs e)
        {
            bool attached = AttachConsole(-1);            
                        
            CommandLine.Parse();
 
            if (!CommandLine.Unhandled)
            {

                if (attached)
                    FreeConsole();
                // If handled, exit the application
                Environment.Exit(0);
            }
                       

            var pack = new FilePackager();
            var exeFile = Assembly.GetExecutingAssembly().Location;

            if (pack.FindMarkerOffset(exeFile, pack.SeparatorBytes) > 0)
            {
                var outputPath = Path.Combine(Path.GetTempPath(), "dm_" + StringUtils.GenerateUniqueId(8));                
                TempUnpackDirectory = outputPath;
                                
                if (!pack.UnpackageFile(exeFile, outputPath, true))
                {
                    if (!attached)
                    {
                        MessageBox.Show("An error occurred unpacking the viewer app and Web site.\n" +
                                        pack.ErrorMessage, "Web Viewer Error", MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                    else
                        Console.WriteLine("\n❌ Error:\n" + pack.ErrorMessage);

                    Environment.Exit(1);
                }
                Environment.CurrentDirectory = outputPath;

                var exe = Path.Combine(outputPath, "WebPackageViewer.exe");
                var p = Process.Start(new ProcessStartInfo() { FileName = exe, WorkingDirectory = outputPath });

                Console.Write("\n✅ Launching Web Viewer...");

                if (attached)
                    FreeConsole();

                Environment.Exit(0);
            }

            InitialStartDirectory = AppContext.BaseDirectory.TrimEnd('/');
            InitialUserStartedDirectory = Environment.CurrentDirectory;

            // Read configuration from Json and override with explicit values passed
            var config = WebViewerConfiguration.Read();

            if (!string.IsNullOrEmpty(CommandLine.VirtualPath))            
                config.VirtualPath = '/' + CommandLine.VirtualPath.Trim('/');
            else
                config.VirtualPath = '/' + config.VirtualPath.Trim('/');

            if (!string.IsNullOrEmpty(CommandLine.InitialUrl))
                config.InitialUrl = '/' + CommandLine.InitialUrl.Trim('/');
            if (string.IsNullOrEmpty(config.InitialUrl))
                config.InitialUrl = ('/'  + config.VirtualPath + "/index.html").Replace("//","/"); // default to index.html if nothing else specified
            if (string.IsNullOrEmpty(config.WindowTitle))
                config.WindowTitle = "West Wind Web Package Viewer";

            // override from command line
            if (e.Args.Length > 0 && !e.Args[0].StartsWith("-"))
                config.WebRootPath = e.Args[0];
                         
            base.OnStartup(e);
                        
            if (attached)
                FreeConsole();

            MainWindow mainWindow = new MainWindow(config);
            mainWindow.Show();

            App.Current.Exit += (s, args) =>
            {

                if (!string.IsNullOrEmpty(App.TempUnpackDirectory))
                {
                    // Important!
                    //Environment.CurrentDirectory = Path.GetTempPath();
                    //Directory.Delete(App.TempUnpackDirectory, true);

                    var exec = $@"-ExecutionPolicy Bypass  -Command ""start-sleep -milliseconds 2000; remove-item '{App.TempUnpackDirectory}' -recurse -force"";Start-Sleep -Seconds 5";
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = exec,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        WorkingDirectory = Path.GetTempPath()  // Important!
                    });
                    process?.Dispose();

                }
            };
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
    }
}
