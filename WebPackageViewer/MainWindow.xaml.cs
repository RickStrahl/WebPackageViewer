using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WebPackageViewer
{
    public partial class MainWindow : Window
    {        
        public WebViewerConfiguration Configuration { get; private set; }

        public string ExeFile { get; set; }

        public string OutputFolder { get; set; }


        public MainWindow(WebViewerConfiguration config)
        {
            Configuration = config;

            InitializeComponent();

            Loaded += Window_Loaded;
            StateChanged += Window_StateChanged;
            Activated += Window_Activated;
            Deactivated += Window_Deactivated;
            TitleBar.MouseRightButtonUp += TitleBar_MouseRightButtonUp;
            SystemMenuButton.Click += SystemMenuButton_Click;
            SystemMenuButton.MouseDoubleClick += SystemMenuButton_MouseDoubleClick;

            InitWebView();

            ExeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            OutputFolder = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(ExeFile) + "_WebViewer");

            Title = App.CommandLine.WindowTitle ?? Configuration.WindowTitle ?? "West Wind Web Package Viewer";

            if (!string.IsNullOrEmpty(config.WindowSize) && config.WindowSize.Contains("x"))
            {
                var parts = config.WindowSize.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                {
                    Width = width;
                    Height = height;
                }
            }

            Closed += MainWindow_Closed;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        }


        private async void InitWebView()
        {
            try
            {
                var envPath = Path.Combine(Path.GetTempPath(),
                    Path.GetFileNameWithoutExtension("WebViewer") + "_WebView");
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: envPath);
                await webView.EnsureCoreWebView2Async(environment);


                // Handle top level links
                webView.NavigationStarting += (s, e) =>
                {                    
                    var virt = Configuration.VirtualPath.Trim('/');
                    if (App.CommandLine.VirtualPath != null)
                        virt = App.CommandLine.VirtualPath.Trim('/');

                    var uri = e.Uri;
                    if (string.IsNullOrEmpty(virt))
                    {
                        e.Cancel = false ;
                        // just let the control navigate                        
                    }
                    else if (uri.Contains($"/{virt}/"))
                    {                        
                        var newUri = uri.Replace($"/{virt}/", "/");
                        e.Cancel = true;                        
                        webView.CoreWebView2.Navigate(newUri);
                    }
                    else if (uri.EndsWith($"/{virt}"))
                    {
                        var newUri = uri.Replace($"/{virt}", "/");
                        e.Cancel = true;
                        webView.CoreWebView2.Navigate(newUri);
                    }
                };

                // Handled embedded reosource links
                // look at all resource requests and serve local files
                webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All); 
                webView.CoreWebView2.WebResourceRequested += (s, args) =>
                {
                    var uri = new Uri(args.Request.Uri);                    
                    var suri = uri.ToString();

                    var absolutePath = uri.AbsolutePath;                    
                    if (absolutePath == "/")
                        absolutePath = Configuration.InitialUrl;

                    var virt = Configuration.VirtualPath.Trim('/');

                    var localPath = absolutePath.Replace($"/{virt}/", "/").Replace("//", "/");                
                    var filePath = Path.Combine(Configuration.WebRootPath, localPath.TrimStart('/'));

                    if (File.Exists(filePath))
                    {
                        var stream = File.OpenRead(filePath);                        
                        var response = webView.CoreWebView2.Environment.CreateWebResourceResponse(
                            stream, 200, "OK", "Content-Type: " + GetMimeType(filePath));
                        args.Response = response;
                    }
                };                                                                                                                                                                                                                                      

                var vrt = Configuration.VirtualPath.Trim('/');
                var initial = "/" + Configuration.InitialUrl.Trim('/');                    
                webView.Source = new Uri($"https://webviewer.host{vrt}{initial}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 initialization failed: " + ex.Message);
            }
        }

        private static string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (!_mimeTypeMappings.TryGetValue(ext, out string mimeType))
                return "application/octet-stream";

            return mimeType;
        }


        private static IDictionary<string, string> _mimeTypeMappings =
      new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
      {
                #region extension to MIME type list
                {".asf", "video/x-ms-asf"},
                {".asx", "video/x-ms-asf"},
                {".avi", "video/x-msvideo"},
                {".bin", "application/octet-stream"},
                {".cco", "application/x-cocoa"},
                {".crt", "application/x-x509-ca-cert"},
                {".css", "text/css"},
                {".deb", "application/octet-stream"},
                {".der", "application/x-x509-ca-cert"},
                {".dll", "application/octet-stream"},
                {".dmg", "application/octet-stream"},
                {".ear", "application/java-archive"},
                {".eot", "application/octet-stream"},
                {".exe", "application/octet-stream"},
                {".flv", "video/x-flv"},
                {".gif", "image/gif"},
                {".hqx", "application/mac-binhex40"},
                {".htc", "text/x-component"},
                {".htm", "text/html"},
                {".html", "text/html"},
                {".ico", "image/x-icon"},
                {".img", "application/octet-stream"},
                {".iso", "application/octet-stream"},
                {".jar", "application/java-archive"},
                {".jardiff", "application/x-java-archive-diff"},
                {".jng", "image/x-jng"},
                {".jnlp", "application/x-java-jnlp-file"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".js", "application/x-javascript"},
                {".mml", "text/mathml"},
                {".mng", "video/x-mng"},
                {".mov", "video/quicktime"},
                {".mp3", "audio/mpeg"},
                {".mpeg", "video/mpeg"},
                {".mpg", "video/mpeg"},
                {".msi", "application/octet-stream"},
                {".msm", "application/octet-stream"},
                {".msp", "application/octet-stream"},
                {".pdb", "application/x-pilot"},
                {".pdf", "application/pdf"},
                {".pem", "application/x-x509-ca-cert"},
                {".pl", "application/x-perl"},
                {".pm", "application/x-perl"},
                {".png", "image/png"},
                {".prc", "application/x-pilot"},
                {".ra", "audio/x-realaudio"},
                {".rar", "application/x-rar-compressed"},
                {".rpm", "application/x-redhat-package-manager"},
                {".rss", "text/xml"},
                {".run", "application/x-makeself"},
                {".sea", "application/x-sea"},
                {".shtml", "text/html"},
                {".sit", "application/x-stuffit"},
                {".svg", "image/svg+xml" },
                {".swf", "application/x-shockwave-flash"},
                {".tcl", "application/x-tcl"},
                {".tk", "application/x-tcl"},
                {".txt", "text/plain"},
                {".war", "application/java-archive"},
                {".wbmp", "image/vnd.wap.wbmp"},
                {".wmv", "video/x-ms-wmv"},
                {".woff", "application/font-woff"},
                {".woff2", "application/font-woff2"},
                {".xml", "text/xml"},
                {".xpi", "application/x-xpinstall"},
                {".zip", "application/zip"}
          #endregion
      };

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyThemeFromSystem();
            UpdateMaximizeRestoreGlyph();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            UpdateMaximizeRestoreGlyph();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            UpdateActivationVisualState(true);
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            UpdateActivationVisualState(false);
        }

        private void SystemMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var point = PointToScreen(new Point(0, TitleBar.ActualHeight));
            SystemCommands.ShowSystemMenu(this, point);
        }

        private void SystemMenuButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var screenPoint = PointToScreen(e.GetPosition(this));
            SystemCommands.ShowSystemMenu(this, screenPoint);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void UpdateMaximizeRestoreGlyph()
        {
            if (MaximizeRestoreButton == null)
                return;

            MaximizeRestoreButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.Color ||
                e.Category == UserPreferenceCategory.General ||
                e.Category == UserPreferenceCategory.VisualStyle)
            {
                Dispatcher.Invoke(ApplyThemeFromSystem);
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            Activated -= Window_Activated;
            Deactivated -= Window_Deactivated;
            Closed -= MainWindow_Closed;
        }

        private void ApplyThemeFromSystem()
        {
            var isLightTheme = IsLightThemeEnabled();

            SetBrushColor("WindowBackgroundBrush", isLightTheme ? Color.FromRgb(248, 248, 248) : Color.FromRgb(45, 45, 48));
            SetBrushColor("WindowBorderBrush", Colors.Transparent);
            SetBrushColor("ActiveTitleBarBackgroundBrush", isLightTheme ? Color.FromRgb(245, 245, 245) : Color.FromRgb(30, 30, 30));
            SetBrushColor("TitleBarInactiveBackgroundBrush", isLightTheme ? Color.FromRgb(241, 241, 241) : Color.FromRgb(36, 36, 38));
            SetBrushColor("TitleBarForegroundBrush", isLightTheme ? Color.FromRgb(32, 32, 32) : Color.FromRgb(245, 245, 245));
            SetBrushColor("TitleBarButtonBackgroundBrush", Colors.Transparent);
            SetBrushColor("TitleBarButtonHoverBrush", isLightTheme ? Color.FromArgb(18, 0, 0, 0) : Color.FromArgb(36, 255, 255, 255));
            SetBrushColor("TitleBarButtonPressedBrush", isLightTheme ? Color.FromArgb(34, 0, 0, 0) : Color.FromArgb(54, 255, 255, 255));
            SetBrushColor("TitleBarButtonForegroundBrush", isLightTheme ? Color.FromRgb(32, 32, 32) : Color.FromRgb(238, 238, 238));

            UpdateActivationVisualState(IsActive);
        }

        private void SetBrushColor(string resourceKey, Color color)
        {
            Resources[resourceKey] = new SolidColorBrush(color);
        }

        private void UpdateActivationVisualState(bool isActive)
        {
            if (TitleBar == null)
                return;

            var key = isActive ? "ActiveTitleBarBackgroundBrush" : "TitleBarInactiveBackgroundBrush";
            if (Resources[key] is SolidColorBrush brush)
                TitleBar.Background = brush;
        }

        private static bool IsLightThemeEnabled()
        {            
            const string personalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            using (var key = Registry.CurrentUser.OpenSubKey(personalizeKey))
            {
                var value = key?.GetValue("AppsUseLightTheme");
                if (value is int appTheme)
                    return appTheme > 0;
            }

            return true;
        }
    }
}
