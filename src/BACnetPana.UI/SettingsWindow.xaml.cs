using bacneTPana.Core;
using bacneTPana.Models;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

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

            // Lade Farben
            UpdateColorLabels();

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

            // Farben werden bereits in _config gespeichert über die Color-Button-Click-Handler

            try
            {
                _configService.SaveCOVThresholds(_config);
                System.Windows.MessageBox.Show(
                    "Einstellungen erfolgreich gespeichert.",
                    "Erfolg",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
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
            var result = System.Windows.MessageBox.Show(
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

                // Reset colors
                _config.GreenColor = "#28A745";
                _config.YellowColor = "LightBlue";
                _config.RedColor = "#FF9800";
                _config.CriticalColor = "#C62828";
                UpdateColorLabels();
            }
        }

        private void UpdateColorLabels()
        {
            if (GreenColorLabel != null)
                GreenColorLabel.Foreground = ColorBrushFromString(_config.GreenColor);
            if (GreenColorSwatch != null)
                GreenColorSwatch.Background = ColorBrushFromString(_config.GreenColor);
            if (YellowColorLabel != null)
                YellowColorLabel.Foreground = ColorBrushFromString(_config.YellowColor);
            if (YellowColorSwatch != null)
                YellowColorSwatch.Background = ColorBrushFromString(_config.YellowColor);
            if (RedColorLabel != null)
                RedColorLabel.Foreground = ColorBrushFromString(_config.RedColor);
            if (RedColorSwatch != null)
                RedColorSwatch.Background = ColorBrushFromString(_config.RedColor);
            if (CriticalColorLabel != null)
                CriticalColorLabel.Foreground = ColorBrushFromString(_config.CriticalColor);
            if (CriticalColorSwatch != null)
                CriticalColorSwatch.Background = ColorBrushFromString(_config.CriticalColor);
        }

        private Brush ColorBrushFromString(string colorString)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        private void GreenColorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            SelectColor("Gut-Farbe wählen", _config.GreenColor, color => _config.GreenColor = color);
        }

        private void YellowColorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            SelectColor("OK-Farbe wählen", _config.YellowColor, color => _config.YellowColor = color);
        }

        private void RedColorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            SelectColor("Schlecht-Farbe wählen", _config.RedColor, color => _config.RedColor = color);
        }

        private void CriticalColorBorder_Click(object sender, MouseButtonEventArgs e)
        {
            SelectColor("Kritisch-Farbe wählen", _config.CriticalColor, color => _config.CriticalColor = color);
        }

        private void SelectColor(string title, string currentColor, System.Action<string> onColorSelected)
        {
            // Erstelle ein RGB-ColorPicker-Dialog-Fenster
            var colorDialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 300,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            var mainPanel = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(20, 20, 20, 0)
            };

            // Rot-Slider mit Label und Wert in einer Zeile
            var redHeader = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 3)
            };
            var redLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Rot (R):",
                Width = 80
            };
            var redValue = new System.Windows.Controls.TextBlock
            {
                Text = "128",
                Margin = new Thickness(10, 0, 0, 0)
            };
            redHeader.Children.Add(redLabel);
            redHeader.Children.Add(redValue);

            var redSlider = new System.Windows.Controls.Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Grün-Slider mit Label und Wert in einer Zeile
            var greenHeader = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 3)
            };
            var greenLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Grün (G):",
                Width = 80
            };
            var greenValue = new System.Windows.Controls.TextBlock
            {
                Text = "128",
                Margin = new Thickness(10, 0, 0, 0)
            };
            greenHeader.Children.Add(greenLabel);
            greenHeader.Children.Add(greenValue);

            var greenSlider = new System.Windows.Controls.Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Blau-Slider mit Label und Wert in einer Zeile
            var blueHeader = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 3)
            };
            var blueLabel = new System.Windows.Controls.TextBlock
            {
                Text = "Blau (B):",
                Width = 80
            };
            var blueValue = new System.Windows.Controls.TextBlock
            {
                Text = "128",
                Margin = new Thickness(10, 0, 0, 0)
            };
            blueHeader.Children.Add(blueLabel);
            blueHeader.Children.Add(blueValue);

            var blueSlider = new System.Windows.Controls.Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = 128,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Hex-Label für Farb-Vorschau
            var hexLabel = new System.Windows.Controls.TextBlock
            {
                Text = "#808080",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };

            // Farb-Vorschau mit Hex-Label darin
            var previewBorder = new System.Windows.Controls.Border
            {
                Height = 40,
                Margin = new Thickness(0, 0, 0, 0),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                Child = hexLabel
            };

            // Setze Startfarbe anhand der aktuellen Farbe
            var initialColor = TryParseColor(currentColor);
            redSlider.Value = initialColor.R;
            greenSlider.Value = initialColor.G;
            blueSlider.Value = initialColor.B;

            // Update-Funktion
            System.Action updatePreview = () =>
            {
                byte r = (byte)redSlider.Value;
                byte g = (byte)greenSlider.Value;
                byte b = (byte)blueSlider.Value;

                redValue.Text = r.ToString();
                greenValue.Text = g.ToString();
                blueValue.Text = b.ToString();

                var bgColor = Color.FromRgb(r, g, b);
                previewBorder.Background = new SolidColorBrush(bgColor);
                hexLabel.Text = $"#{r:X2}{g:X2}{b:X2}";

                // Text-Farbe für bessere Lesbarkeit anpassen
                var brightness = (r * 0.299 + g * 0.587 + b * 0.114);
                hexLabel.Foreground = brightness > 128 ? Brushes.Black : Brushes.White;
            };

            redSlider.ValueChanged += (s, e) => updatePreview();
            greenSlider.ValueChanged += (s, e) => updatePreview();
            blueSlider.ValueChanged += (s, e) => updatePreview();

            updatePreview();

            // Buttons
            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "Abbrechen",
                Width = 80,
                Padding = new Thickness(10, 5, 10, 5)
            };

            okButton.Click += (s, e) =>
            {
                byte r = (byte)redSlider.Value;
                byte g = (byte)greenSlider.Value;
                byte b = (byte)blueSlider.Value;
                var hexColor = $"#{r:X2}{g:X2}{b:X2}";
                onColorSelected(hexColor);
                UpdateColorLabels();
                colorDialog.DialogResult = true;
                colorDialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                colorDialog.DialogResult = false;
                colorDialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            // Alle Controls hinzufügen
            mainPanel.Children.Add(redHeader);
            mainPanel.Children.Add(redSlider);
            mainPanel.Children.Add(greenHeader);
            mainPanel.Children.Add(greenSlider);
            mainPanel.Children.Add(blueHeader);
            mainPanel.Children.Add(blueSlider);
            mainPanel.Children.Add(previewBorder);

            var scrollViewer = new System.Windows.Controls.ScrollViewer
            {
                Content = mainPanel,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
            };

            var root = new System.Windows.Controls.DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(10, 10, 10, 5)
            };

            System.Windows.Controls.DockPanel.SetDock(buttonPanel, System.Windows.Controls.Dock.Bottom);
            root.Children.Add(buttonPanel);
            root.Children.Add(scrollViewer);

            colorDialog.Content = root;
            colorDialog.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    colorDialog.DialogResult = false;
                    colorDialog.Close();
                }
            };
            colorDialog.ShowDialog();
        }

        private Color TryParseColor(string colorString)
        {
            try
            {
                var colorObj = ColorConverter.ConvertFromString(colorString);
                if (colorObj is Color color)
                    return color;
            }
            catch
            {
                // ignore
            }

            return Colors.Gray;
        }
    }
}
