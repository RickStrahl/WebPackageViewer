using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

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
            InitWebView();

            ExeFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            OutputFolder = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(ExeFile) + "_WebViewer");

            Title = Configuration.WindowTitle;
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
                        //webView.CoreWebView2.Navigate(uri);                        
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
                    if (!suri.StartsWith("https://webviewer.host"))
                        return;

                    var virt = Configuration.VirtualPath.Trim('/');

                    var localPath = uri.AbsolutePath.Replace($"/{virt}/", "/").Replace("//", "/");                
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
    }
}
