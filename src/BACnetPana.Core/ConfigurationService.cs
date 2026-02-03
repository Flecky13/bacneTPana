using bacneTPana.Models;
using System.Text.Json;

namespace bacneTPana.Core
{
    /// <summary>
    /// Service zur Verwaltung von Anwendungskonfigurationen
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configPath;
        private readonly string _configFile = "cov_thresholds.json";

        public ConfigurationService()
        {
            // Erstelle Konfigurationsverzeichnis in APPDATA
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _configPath = Path.Combine(appDataPath, "bacneTPana");

            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }
        }

        /// <summary>
        /// Lädt die COV Threshold Konfiguration aus dem APPDATA-Verzeichnis
        /// </summary>
        public COVThresholdConfig LoadCOVThresholds()
        {
            try
            {
                var filePath = Path.Combine(_configPath, _configFile);

                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var config = JsonSerializer.Deserialize<COVThresholdConfig>(json);
                    return config ?? new COVThresholdConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Konfiguration: {ex.Message}");
            }

            return new COVThresholdConfig();
        }

        /// <summary>
        /// Speichert die COV Threshold Konfiguration im APPDATA-Verzeichnis
        /// </summary>
        public void SaveCOVThresholds(COVThresholdConfig config)
        {
            try
            {
                var filePath = Path.Combine(_configPath, _configFile);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Speichern der Konfiguration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gibt den Pfad zum Konfigurationsverzeichnis zurück
        /// </summary>
        public string GetConfigPath() => _configPath;

        /// <summary>
        /// Gibt den vollständigen Pfad zur Konfigurationsdatei zurück
        /// </summary>
        public string GetConfigFilePath() => Path.Combine(_configPath, _configFile);
    }
}
