using bacneTPana.Core;
using bacneTPana.Models;
using System.Windows;

namespace bacneTPana.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ConfigurationService _configService;
        private COVThresholdConfig _config;

        public SettingsWindow()
        {
            InitializeComponent();
            _configService = new ConfigurationService();
            _config = _configService.LoadCOVThresholds();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // COV Slider
            GreenThresholdSlider.Value = _config.GreenThreshold;
            YellowThresholdSlider.Value = _config.YellowThreshold;
            RedThresholdSlider.Value = _config.RedThreshold;

            // ReadProperty Slider
            ReadPropertyGreenThresholdSlider.Value = _config.ReadPropertyGreenThreshold;
            ReadPropertyYellowThresholdSlider.Value = _config.ReadPropertyYellowThreshold;
            ReadPropertyRedThresholdSlider.Value = _config.ReadPropertyRedThreshold;

            UpdateValueLabels();

            // Subscribe to slider changes
            GreenThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            YellowThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            RedThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            ReadPropertyGreenThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            ReadPropertyYellowThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            ReadPropertyRedThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            GreenThresholdValueLabel.Text = GreenThresholdSlider.Value.ToString("F0");
            YellowThresholdValueLabel.Text = YellowThresholdSlider.Value.ToString("F0");
            RedThresholdValueLabel.Text = RedThresholdSlider.Value.ToString("F0");
            ReadPropertyGreenThresholdValueLabel.Text = ReadPropertyGreenThresholdSlider.Value.ToString("F0");
            ReadPropertyYellowThresholdValueLabel.Text = ReadPropertyYellowThresholdSlider.Value.ToString("F0");
            ReadPropertyRedThresholdValueLabel.Text = ReadPropertyRedThresholdSlider.Value.ToString("F0");
        }

        private bool ValidateSettings()
        {
            // Validate COV settings
            int greenValue = (int)GreenThresholdSlider.Value;
            int yellowValue = (int)YellowThresholdSlider.Value;
            int redValue = (int)RedThresholdSlider.Value;

            if (greenValue >= yellowValue)
            {
                MessageBox.Show(
                    "Der Grüne Schwellwert (COV) muss kleiner als der Gelbe Schwellwert sein.",
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (yellowValue >= redValue)
            {
                MessageBox.Show(
                    "Der Gelbe Schwellwert (COV) muss kleiner als der Rote Schwellwert sein.",
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // Validate ReadProperty settings
            int rpGreenValue = (int)ReadPropertyGreenThresholdSlider.Value;
            int rpYellowValue = (int)ReadPropertyYellowThresholdSlider.Value;
            int rpRedValue = (int)ReadPropertyRedThresholdSlider.Value;

            if (rpGreenValue >= rpYellowValue)
            {
                MessageBox.Show(
                    "Der Grüne Schwellwert (ReadProperty) muss kleiner als der Gelbe Schwellwert sein.",
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (rpYellowValue >= rpRedValue)
            {
                MessageBox.Show(
                    "Der Gelbe Schwellwert (ReadProperty) muss kleiner als der Rote Schwellwert sein.",
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateSettings())
                return;

            // Save COV settings
            _config.GreenThreshold = (int)GreenThresholdSlider.Value;
            _config.YellowThreshold = (int)YellowThresholdSlider.Value;
            _config.RedThreshold = (int)RedThresholdSlider.Value;

            // Save ReadProperty settings
            _config.ReadPropertyGreenThreshold = (int)ReadPropertyGreenThresholdSlider.Value;
            _config.ReadPropertyYellowThreshold = (int)ReadPropertyYellowThresholdSlider.Value;
            _config.ReadPropertyRedThreshold = (int)ReadPropertyRedThresholdSlider.Value;

            try
            {
                _configService.SaveCOVThresholds(_config);
                MessageBox.Show(
                    "Einstellungen erfolgreich gespeichert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fehler beim Speichern der Einstellungen:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Möchten Sie die Einstellungen auf die Standardwerte zurücksetzen?\n\n" +
                "COV - Grün: 2, Gelb: 10, Rot: 30\n" +
                "ReadProperty - Grün: 6, Gelb: 30, Rot: 60",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Reset COV settings
                GreenThresholdSlider.Value = 2;
                YellowThresholdSlider.Value = 10;
                RedThresholdSlider.Value = 30;

                // Reset ReadProperty settings
                ReadPropertyGreenThresholdSlider.Value = 6;
                ReadPropertyYellowThresholdSlider.Value = 30;
                ReadPropertyRedThresholdSlider.Value = 60;
            }
        }
    }
}
