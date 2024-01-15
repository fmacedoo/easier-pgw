using System.Text.Json;

namespace AppPDV
{
    public class AppSettings
    {
        public PGWSettings? PGWSettings { get; set; }
    }

    public class PGWSettings
    {
        public string? SENHA_TECNICA { get; set; }
        public string? PONTO_CAPTURA { get; set; }
        public string? CPNJ { get; set; }
        public string? SERVIDOR { get; set; }
    }

    public class ConfigurationManager
    {
        public static AppSettings? LoadAppSettings()
        {
            string json = File.ReadAllText("appsettings.json");
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings;
        }
    }

}