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
            GreenThresholdSlider.Value = _config.GreenThreshold;
            YellowThresholdSlider.Value = _config.YellowThreshold;
            RedThresholdSlider.Value = _config.RedThreshold;

            UpdateValueLabels();

            // Subscribe to slider changes
            GreenThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            YellowThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
            RedThresholdSlider.ValueChanged += (s, e) => UpdateValueLabels();
        }

        private void UpdateValueLabels()
        {
            GreenThresholdValueLabel.Text = GreenThresholdSlider.Value.ToString("F0");
            YellowThresholdValueLabel.Text = YellowThresholdSlider.Value.ToString("F0");
            RedThresholdValueLabel.Text = RedThresholdSlider.Value.ToString("F0");
        }

        private bool ValidateSettings()
        {
            int greenValue = (int)GreenThresholdSlider.Value;
            int yellowValue = (int)YellowThresholdSlider.Value;
            int redValue = (int)RedThresholdSlider.Value;

            if (greenValue >= yellowValue)
            {
                MessageBox.Show(
                    "Der Grüne Schwellwert muss kleiner als der Gelbe Schwellwert sein.",
                    "Validierungsfehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            if (yellowValue >= redValue)
            {
                MessageBox.Show(
                    "Der Gelbe Schwellwert muss kleiner als der Rote Schwellwert sein.",
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

            _config.GreenThreshold = (int)GreenThresholdSlider.Value;
            _config.YellowThreshold = (int)YellowThresholdSlider.Value;
            _config.RedThreshold = (int)RedThresholdSlider.Value;

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
                "Grün: 2, Gelb: 10, Rot: 30",
                "Bestätigung",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                GreenThresholdSlider.Value = 2;
                YellowThresholdSlider.Value = 10;
                RedThresholdSlider.Value = 30;
                UpdateValueLabels();
            }
        }
    }
}
