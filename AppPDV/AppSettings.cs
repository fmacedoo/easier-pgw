using System.Net;
using System.Text.Json;

namespace AppPDV
{
    public class AppSettings
    {
        public AppSettings()
        {
            WebView = new WebViewSettings();
        }

        public PGWSettings? PGW { get; set; }
        public WebViewSettings WebView { get; set; }

        public static AppSettings Instance { get; private set; } = Load();

        private static AppSettings Load()
        {
            string json = File.ReadAllText("appsettings.json");
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
            settings ??= new AppSettings();
            return settings;
        }

        public static void Persist()
        {
            var json = JsonSerializer.Serialize(Instance);
            File.WriteAllText("appsettings.json", json);
        }
    }

    public class WebViewSettings
    {
        public string WebClientAddress { get; set; } = "http://localhost:3000";
        public string? FormSize { get; set; }
    }

    public class PGWSettings
    {
        public string? SENHA_TECNICA { get; set; }
        public string? PONTO_CAPTURA { get; set; }
        public string? CPNJ { get; set; }
        public string? SERVIDOR { get; set; }
    }
}