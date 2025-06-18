using System;
using System.IO;
using WebPackageViewer.Utilities;

namespace WebPackageViewer
{
    public class WebViewerConfiguration
    {
        public string WebRootPath { get; set; } = null;
        public string VirtualPath { get; set; } = "/";
        public string InitialUrl { get; set; } = "/index.html";

        public string WindowTitle { get; set; } = "West Wind Web Package Viewer";

        public WebViewerConfiguration(string webRootPath = null, string virtualPath = null)
        {
            if (!string.IsNullOrEmpty(webRootPath))
                WebRootPath = webRootPath;
            else
                WebRootPath = App.InitialUserStartedDirectory;

            if (!string.IsNullOrEmpty(virtualPath))
                VirtualPath = virtualPath;
           
        }

        public static WebViewerConfiguration Read(WebViewerConfiguration existingConfig = null)
        {
            var config = new WebViewerConfiguration();

            var configFilePath = Path.Combine(App.InitialUserStartedDirectory, "WebPackageViewer.config.json");
            if (File.Exists(configFilePath))
            {
                var json = File.ReadAllText(configFilePath);                
                config.WebRootPath = json.ExtractString("\"WebRootPath\": \"", "\",", false)?.Trim();
                config.InitialUrl = json.ExtractString("\"InitialUrl\": \"", "\"", false)?.Trim();
                config.VirtualPath = json.ExtractString("\"VirtualPath\": \"", "\"", false)?.Trim();
                config.WindowTitle = json.ExtractString("\"WindowTitle\": \"", "\"", false)?.Trim();                
            }

            if (existingConfig != null)
            {
                if (!string.IsNullOrEmpty(existingConfig.WebRootPath))
                    config.WebRootPath = existingConfig.WebRootPath;
                if (!string.IsNullOrEmpty(existingConfig.VirtualPath))
                    config.VirtualPath = existingConfig.VirtualPath;
                if (!string.IsNullOrEmpty(existingConfig.InitialUrl))
                    config.InitialUrl = existingConfig.InitialUrl;
                if (!string.IsNullOrEmpty(existingConfig.WindowTitle))
                    config.WindowTitle = existingConfig.WindowTitle;
            }

            return config;
        }
    }
}
