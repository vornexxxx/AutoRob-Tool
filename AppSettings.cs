
using System.IO;


namespace ELRCRobTool
{
    /// <summary>
    /// Provides global static access to application settings.
    /// </summary>
    public static class AppSettings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static Settings Config { get; private set; } = new();

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    Config = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
                    Logger.WriteLine("i ~ Settings loaded from config.json.");
                }
                catch (Exception ex)
                {
                    Logger.WriteLine($"! ~ Failed to load settings: {ex.Message}. Using defaults.");
                    Config = new Settings();
                }
            }
            else
            {
                Logger.WriteLine("i ~ No config file found. Using default settings.");
                Config = new Settings();
            }
        }

        public static void Save()
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(Config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                Logger.WriteLine("i ~ Settings saved to config.json.");
            }
            catch (Exception ex)
            {
                Logger.WriteLine($"! ~ Failed to save settings: {ex.Message}");
            }
        }
    }
}