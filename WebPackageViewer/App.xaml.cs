using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using WebPackageViewer.CommandLine;
using WebPackageViewer.Utilities;


namespace WebPackageViewer
{
    public partial class App : Application
    {
        public static string InitialStartDirectory { get; set; }

        public static string InitialUserStartedDirectory { get; set; }

        public static string TempUnpackDirectory { get; set; }

        public static WebPackageViewerCommandLine CommandLine { get; set; } = new WebPackageViewerCommandLine();

        public static bool IsConsoleApp { get; set;  }
      

        protected override void OnStartup(StartupEventArgs e)
        {            
            IsConsoleApp =  AttachConsole(-1);


            if (IsConsoleApp)
            {
                // delay slightly to let existing prompt finish and then show our prompt
                System.Threading.Thread.Sleep(20);
                // Jump over the secondary prompt so we start our output on clean new line
                Console.WriteLine();
            }

            InitialStartDirectory = AppContext.BaseDirectory.TrimEnd('/');
            InitialUserStartedDirectory = Environment.CurrentDirectory;

            CommandLine.Parse();

            if (!CommandLine.Unhandled)
            {
                if (IsConsoleApp)
                    ReleaseConsolePrompt();

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
                    MessageBox.Show("An error occurred unpacking the viewer app and Web site.\n" +
                                    pack.ErrorMessage, "Web Viewer Error", MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                             
                    Environment.Exit(1);
                }
                Environment.CurrentDirectory = outputPath;

                var exe = Path.Combine(outputPath, "WebPackageViewer.exe");
                var p = Process.Start(new ProcessStartInfo() { FileName = exe, WorkingDirectory = outputPath });

                Console.Write("\n✅ Launching Web Viewer...");

                if (IsConsoleApp)
                    ReleaseConsolePrompt();
                Environment.Exit(0);
            }

            if (IsConsoleApp)
                ReleaseConsolePrompt();


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
                        
            

            var exePath = Path.Combine(InitialStartDirectory, "WebView2Loader.dll");
            if (!File.Exists(exePath))
            {
                // If the loader is not present, we may be running from a single file bundle and need to unpack first
                try
                {
                    var loaderBytes = ResourceHelper.LoadWebView2LoaderBytes();
                    File.WriteAllBytes("WebView2Loader.dll", loaderBytes);
                }
                catch
                {
                    MessageBox.Show(
                        """
                        An error occurred unpacking the WebView2Loader.dll resource.

                        Make sure the application is not running from a read-only location and that you have permissions to write to the current directory.

                        Alternately manually copy `WebView2Loader.dll` from the same folder as the WebPackageViewer.exe to the current directory and restart the application.`
                        """,
                        "Web Viewer Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Environment.Exit(1);
                }
            }

            
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

       

        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int cmd);


        const byte VK_RETURN = 0x0D;


        [DllImport("user32.dll")] static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

        static void ReleaseConsolePrompt()
        {
            Console.WriteLine();  // force another line break so that the prompt is on a new line
            FreeConsole();
            keybd_event(VK_RETURN, 0, 0, 0);         // key down
            keybd_event(VK_RETURN, 0, 0x0002, 0);    // key up (KEYEVENTF_KEYUP)
        }

        static bool StartedFromConsole()
        {
            if (AttachConsole(-1))
            {
                FreeConsole();
                return true;
            }

            // Already attached to a console also means console-launched.
            return Marshal.GetLastWin32Error() == 5;
        }
    }


    public static class ResourceHelper
    {
        /// <summary>
        /// Retrieve WebView2Loader.dll which we can't embed into the exe
        /// directly with ILMerge.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static byte[] LoadWebView2LoaderBytes()
        {
            var asm = typeof(ResourceHelper).Assembly;

            using var resStream =
                asm.GetManifestResourceStream("WebPackageViewer.g.resources");

            if (resStream == null)
                throw new FileNotFoundException("WebPackageViewer.g.resources not found.");

            using var reader = new ResourceReader(resStream);

            foreach (DictionaryEntry entry in reader)
            {
                if ((string)entry.Key == "webview2loader.dll")
                {
                    using var stream = (Stream)entry.Value;
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }

            throw new FileNotFoundException("WebPackageViewer.g.resources not found.");
        }
    }

    
}
