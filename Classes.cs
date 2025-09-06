using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tigris
{
    public class AppConfig
    {
        public float Volume { get; set; } = 0.4f;
        public string GameDirectory { get; set; } = "";
        public string WemFolder { get; set; } = "";
        public bool ExportAsWav { get; set; } = false;
        public bool DarkTheme { get; set; } = false;
        public ExportType ExportType { get; set; } = ExportType.Wem;
        public ExportNameType ExportNameType { get; set; } = ExportNameType.Id;
    }
    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public AppConfig Config { get; private set; }

        public ConfigManager()
        {
            Config = LoadConfig();
        }

        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
            }

            return new AppConfig();
        }
        public void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(Config, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
    public class SoundItem
    {
        public string DisplayName { get; set; }
        public string FilePath { get; set; }
        public bool Selected { get; set; } = false;
        public string Language { get; set; } = "idk";

        public long Size { get; set; } = 0;
        public string FormattedSize { get; set; } = "0 B";

        public Dictionary<string, string> Subtitles { get; set; } = new Dictionary<string, string>();
        public string SubtitleText { get; set; }
    }
    
    public enum ExportType
    {
        Wem,
        Wav,
        WemAndWav
    }

    public enum ExportNameType
    {
        Id,
        DisplayName
    } 
}
