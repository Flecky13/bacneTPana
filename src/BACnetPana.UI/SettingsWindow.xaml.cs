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
            UpdateSliderConstraints();

            // Subscribe to slider changes
            GreenThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
            YellowThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
            RedThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
            ReadPropertyGreenThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
            ReadPropertyYellowThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
            ReadPropertyRedThresholdSlider.ValueChanged += (s, e) =>
            {
                UpdateValueLabels();
                UpdateSliderConstraints();
            };
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

        private void UpdateSliderConstraints()
        {
            // COV Sliders
            // Green Slider: Minimum bleibt 0, Maximum wird vom Yellow Slider bestimmt
            GreenThresholdSlider.Maximum = YellowThresholdSlider.Value;

            // Yellow Slider: Minimum wird vom Green Slider bestimmt, Maximum vom Red Slider
            YellowThresholdSlider.Minimum = GreenThresholdSlider.Value;
            YellowThresholdSlider.Maximum = RedThresholdSlider.Value;

            // Red Slider: Minimum wird vom Yellow Slider bestimmt
            RedThresholdSlider.Minimum = YellowThresholdSlider.Value;

            // ReadProperty Sliders
            // Green Slider: Minimum bleibt 0, Maximum wird vom Yellow Slider bestimmt
            ReadPropertyGreenThresholdSlider.Maximum = ReadPropertyYellowThresholdSlider.Value;

            // Yellow Slider: Minimum wird vom Green Slider bestimmt, Maximum vom Red Slider
            ReadPropertyYellowThresholdSlider.Minimum = ReadPropertyGreenThresholdSlider.Value;
            ReadPropertyYellowThresholdSlider.Maximum = ReadPropertyRedThresholdSlider.Value;

            // Red Slider: Minimum wird vom Yellow Slider bestimmt
            ReadPropertyRedThresholdSlider.Minimum = ReadPropertyYellowThresholdSlider.Value;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

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
                "COV - Grün: 2, Gelb: 10, Orange: 30\n" +
                "ReadProperty - Grün: 6, Gelb: 30, Orange: 60",
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
