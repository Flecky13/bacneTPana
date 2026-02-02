using bacneTPana.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace bacneTPana.UI
{
    public partial class BACnetAnalysisWindow : Window
    {
        private List<NetworkPacket> _packets;
        private DateTime _minTime;
        private DateTime _maxTime;
        private double _totalDuration;
        private readonly PlotController _noWheelController = new PlotController();
        private readonly string _activeFilter;
        private DispatcherTimer? _debounceTimer;
        private BACnetDatabase _bacnetDb;
        private List<DeviceBarInfo>? _topDevicesForHover;

        private class DeviceBarInfo
        {
            public string Device { get; set; } = string.Empty;
            public int Count { get; set; }
            public string Id { get; set; } = string.Empty;
            public double Percentage { get; set; }
            public double AveragePerMinute { get; set; }
            public string Ip { get; set; } = string.Empty;
        }

        public BACnetAnalysisWindow(List<NetworkPacket> packets, string activeFilter, BACnetDatabase? bacnetDatabase = null)
        {
            InitializeComponent();
            _packets = packets ?? new List<NetworkPacket>();
            _activeFilter = activeFilter ?? string.Empty;
            _bacnetDb = bacnetDatabase ?? new BACnetDatabase();

            ConfigurePlotController();
            UpdateTitleWithFilter();
            LoadAnalysis();
        }

        private void ConfigurePlotController()
        {
            _noWheelController.UnbindMouseWheel();
        }

        private void LoadAnalysis()
        {
            var packetCount = _packets.Count;
            var totalSize = _packets.Sum(p => p.PacketLength);
            var uniqueProtocols = _packets.Select(p => p.DisplayProtocol).Distinct().Count();

            var timeSpan = "0 s";
            if (_packets.Count >= 2)
            {
                _minTime = _packets.Min(p => p.Timestamp);
                _maxTime = _packets.Max(p => p.Timestamp);
                var duration = _maxTime - _minTime;
                _totalDuration = duration.TotalSeconds;
                timeSpan = $"{_totalDuration:F2} s";
            }
            else if (_packets.Count == 1)
            {
                _minTime = _packets[0].Timestamp;
                _maxTime = _packets[0].Timestamp;
                _totalDuration = 0;
            }

            if (packetCount > 0)
            {
                if (SummaryStartDateTimeLabel != null)
                    SummaryStartDateTimeLabel.Text = FormatDateTimeTwoLines(_minTime);
                if (SummaryEndDateTimeLabel != null)
                    SummaryEndDateTimeLabel.Text = FormatDateTimeTwoLines(_maxTime);
            }
            else
            {
                if (SummaryStartDateTimeLabel != null)
                    SummaryStartDateTimeLabel.Text = "—";
                if (SummaryEndDateTimeLabel != null)
                    SummaryEndDateTimeLabel.Text = "—";
            }

            PacketCountLabel.Text = packetCount.ToString();
            TotalSizeLabel.Text = FormatBytes(totalSize);
            TimeSpanLabel.Text = timeSpan;
            ProtocolCountLabel.Text = uniqueProtocols.ToString();

            if (StartTimeSlider != null && EndTimeSlider != null)
            {
                StartTimeSlider.Maximum = _totalDuration;
                EndTimeSlider.Maximum = _totalDuration;
                StartTimeSlider.Value = 0;
                EndTimeSlider.Value = _totalDuration;

                // UpdateChart() wird automatisch durch ValueChanged-Events der Slider aufgerufen
                // Direkter Aufruf hier entfernt, um doppelte Ausführung zu vermeiden
            }
        }

        private void UpdateChart()
        {
            if (_packets == null || _packets.Count == 0)
                return;
            if (StartTimeSlider == null || EndTimeSlider == null)
                return;

            var startOffset = StartTimeSlider.Value;
            var endOffset = EndTimeSlider.Value;

            if (startOffset >= endOffset)
            {
                return;
            }

            var startTime = _minTime.AddSeconds(startOffset);
            var endTime = _minTime.AddSeconds(endOffset);

            var filteredPackets = _packets
                .Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime)
                .OrderBy(p => p.Timestamp)
                .ToList();
            // Nur BACnet relevante Updates
            UpdateBACnetDatabaseStats();
            UpdateBACnetPacketsPerSecond(filteredPackets);
            UpdateBACnetAnalysis(filteredPackets);
            UpdateBACnetServicesAnalysis(filteredPackets);
            UpdateBACnetReadPropertiesAnalysis(filteredPackets);

            if (StartTimeLabel != null)
                StartTimeLabel.Text = $"{startOffset:F2} s";
            if (EndTimeLabel != null)
                EndTimeLabel.Text = $"{endOffset:F2} s";
            if (DurationLabel != null)
                DurationLabel.Text = $"Anzeigedauer: {(endOffset - startOffset):F2} s";

            if (StartDateTimeLabel != null)
                StartDateTimeLabel.Text = FormatDateTimeTwoLines(startTime);
            if (EndDateTimeLabel != null)
                EndDateTimeLabel.Text = FormatDateTimeTwoLines(endTime);

            if (BACnetSectionBorder != null)
                BACnetSectionBorder.Visibility = Visibility.Visible;
        }

        private void UpdateTitleWithFilter()
        {
            var suffix = string.IsNullOrWhiteSpace(_activeFilter)
                ? "(kein Filter)"
                : $"(Filter: {_activeFilter})";
            if (AnalysisTitleTextBlock != null)
            {
                AnalysisTitleTextBlock.Text = $"Analyse der gefilterten Pakete {suffix}";
            }
        }

        private static string FormatDateTimeTwoLines(DateTime value)
        {
            return value.ToString("yyyy-MM-dd\nHH:mm:ss.fff");
        }

        private void StartTimeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (EndTimeSlider != null && StartTimeSlider != null && StartTimeSlider.Value > EndTimeSlider.Value)
            {
                EndTimeSlider.Value = StartTimeSlider.Value;
            }
            DebounceUpdateChart();
        }

        private void EndTimeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (StartTimeSlider != null && EndTimeSlider != null && EndTimeSlider.Value < StartTimeSlider.Value)
            {
                StartTimeSlider.Value = EndTimeSlider.Value;
            }
            DebounceUpdateChart();
        }

        private void DebounceUpdateChart()
        {
            _debounceTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _debounceTimer.Stop();
            _debounceTimer.Tick -= DebounceTimer_Tick;
            _debounceTimer.Tick += DebounceTimer_Tick;
            _debounceTimer.Start();
        }

        private void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer?.Stop();
            UpdateChart();
        }

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private void PlotView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            if (sender is DependencyObject d)
            {
                var parent = FindScrollViewer(d);
                var sv = parent;
                if (sv != null)
                {
                    var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                    {
                        RoutedEvent = UIElement.MouseWheelEvent,
                        Source = sender
                    };
                    sv.RaiseEvent(args);
                }
            }
        }

        private ScrollViewer? FindScrollViewer(DependencyObject current)
        {
            while (current != null)
            {
                if (current is ScrollViewer sv)
                    return sv;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void TopDevicesChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (TopDevicesChart?.Model == null || _topDevicesForHover == null || _topDevicesForHover.Count == 0)
                return;

            var position = e.GetPosition(TopDevicesChart);
            var categoryAxis = TopDevicesChart.Model.Axes.FirstOrDefault(a => a is CategoryAxis) as CategoryAxis;
            if (categoryAxis == null)
                return;

            // Transformiere Y-Koordinate zu Kategoriendatenindex
            var yData = categoryAxis.InverseTransform(position.Y);
            var index = (int)Math.Round(yData);
            index = Math.Max(0, Math.Min(_topDevicesForHover.Count - 1, index));

            var item = _topDevicesForHover[index];
            var infoText = $"Gerät: {item.Device}\n" +
                           $"Gesamt: {item.Count} Anfragen\n" +
                           $"Durchschnitt: {item.AveragePerMinute:F2}/Min\n" +
                           $"Rel. Verteilung: {item.Percentage:F1}%";

            if (TopDevicesInfoLabel != null)
            {
                TopDevicesInfoLabel.Text = infoText;
            }
        }

        private void TopDevicesChart_MouseLeave(object sender, MouseEventArgs e)
        {
            if (TopDevicesInfoLabel != null)
            {
                TopDevicesInfoLabel.Text = "Bewegen Sie die Maus über einen Balken für Details zum Gerät.";
            }
        }

        // Reuse BACnet-specific update methods from AnalysisWindow implementation
        private void UpdateBACnetAnalysis(List<NetworkPacket> filteredPackets)
        {
            var bacnetPackets = filteredPackets.Where(p =>
                (p.ApplicationProtocol?.ToUpper() == "BACNET") ||
                (p.DestinationPort >= 47808 && p.DestinationPort <= 47823) ||
                (p.SourcePort >= 47808 && p.SourcePort <= 47823)).ToList();

            if (bacnetPackets.Count == 0)
            {
                if (BACnetAnalysisBorder != null)
                    BACnetAnalysisBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (BACnetAnalysisBorder != null)
                BACnetAnalysisBorder.Visibility = Visibility.Visible;

            var broadcastCount = 0;
            var simpleAckCount = 0;      // 2
            var complexAckCount = 0;     // 3
            var whoIsCount = 0;          // 8
            var whoHasCount = 0;         // 7
            var iAmCount = 0;            // 0
            var iHaveCount = 0;          // 1
            var readPropertyCount = 0;   // 12
            var readPropertyMultCount = 0; // 14
            var writePropertyCount = 0;  // 15
            var writePropertyMultCount = 0; // 16
            var subscribeCOVCount = 0;   // 5
            var subscribeCOVPropCount = 0; // 28
            var confCOVNotifCount = 0;   // 1
            var confEventNotifCount = 0; // 2
            var errorCount = 0;          // 5

            foreach (var packet in bacnetPackets)
            {
                if (packet.Details != null)
                {
                    var confirmedCode = GetServiceCode(packet.Details, "BACnet Confirmed Service Code", "BACnet Confirmed Service");
                    var unconfirmedCode = GetServiceCode(packet.Details, "BACnet Unconfirmed Service Code", "BACnet Unconfirmed Service");

                    if (!confirmedCode.HasValue && !unconfirmedCode.HasValue)
                    {
                        var fallbackCode = GetServiceCode(packet.Details, "BACnet Service Code", "BACnet Service");
                        if (fallbackCode.HasValue)
                        {
                            if (packet.Details.TryGetValue("BACnet Type", out var typeValue) && (typeValue?.Contains("Confirmed") == true))
                                confirmedCode = fallbackCode;
                            else
                                unconfirmedCode = fallbackCode;
                        }
                    }

                    if (confirmedCode.HasValue)
                    {
                        switch (confirmedCode.Value)
                        {
                            case 1: confCOVNotifCount++; break;
                            case 2: confEventNotifCount++; break;
                            case 5: subscribeCOVCount++; break;
                            case 12: readPropertyCount++; break;
                            case 14: readPropertyMultCount++; break;
                            case 15: writePropertyCount++; break;
                            case 16: writePropertyMultCount++; break;
                            case 28: subscribeCOVPropCount++; break;
                        }
                    }

                    if (unconfirmedCode.HasValue)
                    {
                        switch (unconfirmedCode.Value)
                        {
                            case 0: iAmCount++; break;
                            case 1: iHaveCount++; break;
                            case 2: simpleAckCount++; break;
                            case 3: complexAckCount++; break;
                            case 5: errorCount++; break;
                            case 7: whoHasCount++; break;
                            case 8: whoIsCount++; break;
                        }
                    }
                }

                if (IsBroadcast(packet))
                {
                    broadcastCount++;
                }
            }

            double durationSeconds = 1;
            if (filteredPackets.Count >= 2)
            {
                var minTs = filteredPackets.Min(p => p.Timestamp);
                var maxTs = filteredPackets.Max(p => p.Timestamp);
                durationSeconds = Math.Max(1e-6, (maxTs - minTs).TotalSeconds);
            }
            var durationMinutes = durationSeconds / 60.0;
            var whoIsPerMinute = durationMinutes > 0 ? whoIsCount / durationMinutes : 0;
            var whoHasPerMinute = durationMinutes > 0 ? whoHasCount / durationMinutes : 0;
            var broadcastPerMinute = durationMinutes > 0 ? broadcastCount / durationMinutes : 0;
            var simpleAckPerMinute = durationMinutes > 0 ? simpleAckCount / durationMinutes : 0;
            var complexAckPerMinute = durationMinutes > 0 ? complexAckCount / durationMinutes : 0;
            var iAmPerMinute = durationMinutes > 0 ? iAmCount / durationMinutes : 0;
            var iHavePerMinute = durationMinutes > 0 ? iHaveCount / durationMinutes : 0;
            var readPropertyPerMinute = durationMinutes > 0 ? readPropertyCount / durationMinutes : 0;
            var readPropertyMultPerMinute = durationMinutes > 0 ? readPropertyMultCount / durationMinutes : 0;
            var writePropertyPerMinute = durationMinutes > 0 ? writePropertyCount / durationMinutes : 0;
            var writePropertyMultPerMinute = durationMinutes > 0 ? writePropertyMultCount / durationMinutes : 0;
            var subscribeCovPerMinute = durationMinutes > 0 ? subscribeCOVCount / durationMinutes : 0;
            var subscribeCovPropPerMinute = durationMinutes > 0 ? subscribeCOVPropCount / durationMinutes : 0;
            var confCovNotifPerMinute = durationMinutes > 0 ? confCOVNotifCount / durationMinutes : 0;
            var confEventNotifPerMinute = durationMinutes > 0 ? confEventNotifCount / durationMinutes : 0;
            var errorPerMinute = durationMinutes > 0 ? errorCount / durationMinutes : 0;

            if (BACnetBroadcastsLabel != null)
                BACnetBroadcastsLabel.Text = $"{broadcastPerMinute:F2}/min ({broadcastCount} total)";
            if (BACnetSimpleAckLabel != null)
                BACnetSimpleAckLabel.Text = $"{simpleAckPerMinute:F2}/min ({simpleAckCount} total)";
            if (BACnetComplexAckLabel != null)
                BACnetComplexAckLabel.Text = $"{complexAckPerMinute:F2}/min ({complexAckCount} total)";
            if (BACnetWhoIsLabel != null)
                BACnetWhoIsLabel.Text = $"{whoIsPerMinute:F2}/min ({whoIsCount} total)";
            if (BACnetWhoHasLabel != null)
                BACnetWhoHasLabel.Text = $"{whoHasPerMinute:F2}/min ({whoHasCount} total)";
            if (BACnetIAmLabel != null)
                BACnetIAmLabel.Text = $"{iAmPerMinute:F2}/min ({iAmCount} total)";
            if (BACnetIHaveLabel != null)
                BACnetIHaveLabel.Text = $"{iHavePerMinute:F2}/min ({iHaveCount} total)";
            if (BACnetReadPropertyLabel != null)
                BACnetReadPropertyLabel.Text = $"{readPropertyPerMinute:F2}/min ({readPropertyCount} total)";
            if (BACnetReadPropertyMultLabel != null)
                BACnetReadPropertyMultLabel.Text = $"{readPropertyMultPerMinute:F2}/min ({readPropertyMultCount} total)";
            if (BACnetWritePropertyLabel != null)
                BACnetWritePropertyLabel.Text = $"{writePropertyPerMinute:F2}/min ({writePropertyCount} total)";
            if (BACnetWritePropertyMultLabel != null)
                BACnetWritePropertyMultLabel.Text = $"{writePropertyMultPerMinute:F2}/min ({writePropertyMultCount} total)";
            if (BACnetSubscribeCOVLabel != null)
                BACnetSubscribeCOVLabel.Text = $"{subscribeCovPerMinute:F2}/min ({subscribeCOVCount} total)";
            if (BACnetSubscribeCOVPropLabel != null)
                BACnetSubscribeCOVPropLabel.Text = $"{subscribeCovPropPerMinute:F2}/min ({subscribeCOVPropCount} total)";
            if (BACnetConfCOVNotifLabel != null)
                BACnetConfCOVNotifLabel.Text = $"{confCovNotifPerMinute:F2}/min ({confCOVNotifCount} total)";
            if (BACnetConfEventNotifLabel != null)
                BACnetConfEventNotifLabel.Text = $"{confEventNotifPerMinute:F2}/min ({confEventNotifCount} total)";
            if (BACnetErrorLabel != null)
                BACnetErrorLabel.Text = $"{errorPerMinute:F2}/min ({errorCount} total)";
        }

        private bool IsBroadcast(NetworkPacket packet)
        {
            var dest = packet.DestinationIp;
            if (string.IsNullOrWhiteSpace(dest))
                return false;
            return dest == "255.255.255.255" || dest.EndsWith(".255");
        }

        private void UpdateBACnetServicesAnalysis(List<NetworkPacket> filteredPackets)
        {
            var bacnetPackets = filteredPackets.Where(p =>
                (p.ApplicationProtocol?.ToUpper() == "BACNET") ||
                (p.DestinationPort >= 47808 && p.DestinationPort <= 47823) ||
                (p.SourcePort >= 47808 && p.SourcePort <= 47823)).ToList();

            if (bacnetPackets.Count == 0)
            {
                if (BACnetTopDevicesBorder != null)
                    BACnetTopDevicesBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (BACnetTopDevicesBorder != null)
                BACnetTopDevicesBorder.Visibility = Visibility.Visible;

            // Berechne Zeitspanne für Durchschnittsberechnung
            var timeSpanMinutes = _totalDuration / 60.0; // _totalDuration ist in Sekunden
            if (StartTimeSlider != null && EndTimeSlider != null)
            {
                timeSpanMinutes = (EndTimeSlider.Value - StartTimeSlider.Value) / 60.0;
            }

            var deviceRequests = bacnetPackets
                .GroupBy(p => string.IsNullOrWhiteSpace(p.SourceIp) ? "Unbekannt" : p.SourceIp)
                .Select(g => new
                {
                    Ip = g.Key,
                    Count = g.Count(),
                    Instance = _bacnetDb.IpToInstance.ContainsKey(g.Key) ? _bacnetDb.IpToInstance[g.Key] ?? string.Empty : string.Empty
                })
                .ToList();

            foreach (var kvp in _bacnetDb.IpToInstance)
            {
                if (deviceRequests.Any(d => d.Ip == kvp.Key))
                    continue;

                deviceRequests.Add(new
                {
                    Ip = kvp.Key,
                    Count = 1,
                    Instance = kvp.Value
                });
            }

            var totalRequestCount = deviceRequests.Sum(x => x.Count);

            var formattedDevices = deviceRequests
                .Select(x => new
                {
                    Device = !string.IsNullOrWhiteSpace(x.Instance)
                        ? $"{x.Instance} ({x.Ip})"
                        : $"nicht ermittelbar ({x.Ip})",
                    Count = x.Count,
                    Id = x.Instance != null ? x.Instance : x.Ip,
                    Percentage = totalRequestCount > 0 ? (x.Count * 100.0) / totalRequestCount : 0,
                    AveragePerMinute = timeSpanMinutes > 0 ? x.Count / timeSpanMinutes : 0,
                    Ip = x.Ip
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var totalDevices = formattedDevices.Count;
            if (TopDevicesCountLabel != null)
            {
                TopDevicesCountLabel.Text = $"Geräte: {totalDevices}";
            }

            var topDevices = formattedDevices
                .Take(10)
                .Select(x => new DeviceBarInfo
                {
                    Device = x.Device,
                    Count = x.Count,
                    Id = x.Id ?? string.Empty,
                    Percentage = x.Percentage,
                    AveragePerMinute = x.AveragePerMinute,
                    Ip = x.Ip
                })
                .ToList();
            _topDevicesForHover = topDevices;
            var barHeight = 22;
            var desiredHeight = Math.Max(10, topDevices.Count) * barHeight;

            var topDevicesModel = new PlotModel
            {
                Title = "BACnet-Geräte (Top 10 Anfragen)",
                Background = OxyColors.White
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                ItemsSource = topDevices,
                LabelField = "Device",
                GapWidth = 0.5
            };
            topDevicesModel.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Anzahl Anfragen",
                MinimumPadding = 0,
                AbsoluteMinimum = 0,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
                MinorGridlineColor = OxyColor.FromRgb(240, 240, 240)
            };
            topDevicesModel.Axes.Add(valueAxis);

            if (TopDevicesChart != null)
            {
                TopDevicesChart.Height = desiredHeight;
            }

            // Erstelle BarItems manuell mit benutzerdefinierten Labels
            var barItems = new List<BarItem>();
            foreach (var device in topDevices)
            {
                var barItem = new BarItem
                {
                    Value = device.Count,
                    CategoryIndex = barItems.Count
                };
                barItems.Add(barItem);
            }

            var series = new BarSeries
            {
                ItemsSource = barItems,
                FillColor = OxyColor.FromRgb(23, 162, 184),
                StrokeColor = OxyColor.FromRgb(18, 130, 147),
                StrokeThickness = 1,
                BarWidth = 0.6,
                LabelPlacement = LabelPlacement.Inside,
                LabelMargin = 2,
                TextColor = OxyColors.White
            };

            // Entferne alte Annotationen und setze benutzerdefinierte Labels mit Anzahl und Prozent
            topDevicesModel.Annotations.Clear();
            for (int i = 0; i < barItems.Count && i < topDevices.Count; i++)
            {
                var device = topDevices[i];
                var customLabel = $"{device.Count} ({device.Percentage:F1}%)";

                // Verwende Annotation für benutzerdefinierte Labels
                var annotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = customLabel,
                    TextPosition = new DataPoint(device.Count / 2.0, i),
                    TextColor = OxyColors.White,
                    Stroke = OxyColors.Transparent,
                    StrokeThickness = 0,
                    FontSize = 11,
                    FontWeight = OxyPlot.FontWeights.Bold,
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle
                };
                topDevicesModel.Annotations.Add(annotation);
            }

            topDevicesModel.Series.Add(series);

            if (TopDevicesChart != null)
            {
                TopDevicesChart.Model = topDevicesModel;
                TopDevicesChart.Controller = _noWheelController;
            }

            // Initialer Hinweistext (kompakt)
            if (TopDevicesInfoLabel != null)
            {
                TopDevicesInfoLabel.Text = "Bewegen Sie die Maus über einen Balken für Details zum Gerät.";
            }
        }

        private void UpdateBACnetReadPropertiesAnalysis(List<NetworkPacket> filteredPackets)
        {
            // DEBUG: Schreibe in Output UND Konsole
            var msg = $"========================================\nUpdateBACnetReadPropertiesAnalysis aufgerufen\n  Gesamt Pakete: {filteredPackets?.Count ?? 0}";
            //System.Diagnostics.Debug.WriteLine(msg);
            //Console.WriteLine(msg);
            //System.Diagnostics.Trace.WriteLine(msg);

            if (filteredPackets == null || filteredPackets.Count == 0)
            {
                //var abortMsg = "  ABBRUCH: Keine Pakete vorhanden!";
                //System.Diagnostics.Debug.WriteLine(abortMsg);
                //Console.WriteLine(abortMsg);
                if (BACnetTopReadPropertyBorder != null)
                    BACnetTopReadPropertyBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var bacnetPackets = filteredPackets.Where(p =>
                (p.ApplicationProtocol?.ToUpper() == "BACNET") ||
                (p.DestinationPort >= 47808 && p.DestinationPort <= 47823) ||
                (p.SourcePort >= 47808 && p.SourcePort <= 47823)).ToList();

            var bacnetMsg = $"  BACnet Pakete: {bacnetPackets.Count}";
            //System.Diagnostics.Debug.WriteLine(bacnetMsg);
            //Console.WriteLine(bacnetMsg);

            if (bacnetPackets.Count == 0)
            {
                //var noBacketMsg = "  ABBRUCH: Keine BACnet-Pakete gefunden!";
                //System.Diagnostics.Debug.WriteLine(noBacketMsg);
                //Console.WriteLine(noBacketMsg);
                if (BACnetTopReadPropertyBorder != null)
                    BACnetTopReadPropertyBorder.Visibility = Visibility.Collapsed;
                return;
            }

            var readPropertyGroups = new Dictionary<string, int>();
            int totalReadPropertyCount = 0;  // Für Gruppierung (ohne Device-Objekte)
            int totalReadPropertyRequests = 0;  // Gesamtzahl aller RP/RPM Confirmed-Requests (inkl. Device)
            int debugCheckedPackets = 0;
            int debugConfirmedReqCount = 0;
            int debugReadPropServiceCount = 0;
            int debugObjectType8Excluded = 0;
            int debugNoObjectTypeFound = 0;
            int debugServiceCode12 = 0;
            int debugServiceCode14 = 0;

            int debugRpmMultiObjectPackets = 0;  // Zähler für RPM-Pakete mit mehreren Objekten
            int debugRpmMultiObjectCount = 0;     // Gesamtzahl der zusätzlichen Objekte in RPMs

// Sample: Zeige die ersten 10 Pakete mit Details
            //System.Diagnostics.Debug.WriteLine($"  --- Beispiel-Pakete (erste 10, besonders auf ServiceChoice achten) ---");
            //int sampleCount = 0;
            //foreach (var samplePacket in bacnetPackets)
            //{
            //    if (sampleCount < 10 && samplePacket.Details != null && samplePacket.Details.Count > 0)
            //    {
            //        System.Diagnostics.Debug.WriteLine($"  Paket #{samplePacket.PacketNumber}:");
            //        if (samplePacket.Details.ContainsKey("BACnet Type"))
            //            System.Diagnostics.Debug.WriteLine($"    BACnet Type: '{samplePacket.Details["BACnet Type"]}'");
            //        else
            //            System.Diagnostics.Debug.WriteLine($"    BACnet Type: NICHT VORHANDEN");

            //        if (samplePacket.Details.ContainsKey("BACnet Confirmed Service Code"))
            //            System.Diagnostics.Debug.WriteLine($"    Confirmed Service Code: '{samplePacket.Details["BACnet Confirmed Service Code"]}'");
            //        else
            //            System.Diagnostics.Debug.WriteLine($"    Confirmed Service Code: NICHT VORHANDEN");

            //        if (samplePacket.Details.ContainsKey("BACnet Confirmed Service"))
            //            System.Diagnostics.Debug.WriteLine($"    Confirmed Service: '{samplePacket.Details["BACnet Confirmed Service"]}'");
            //        else
            //            System.Diagnostics.Debug.WriteLine($"    Confirmed Service: NICHT VORHANDEN");

            //        if (samplePacket.Details.ContainsKey("Object Type"))
            //            System.Diagnostics.Debug.WriteLine($"    Object Type: '{samplePacket.Details["Object Type"]}'");
            //        if (samplePacket.Details.ContainsKey("Instance Number"))
            //            System.Diagnostics.Debug.WriteLine($"    Instance: '{samplePacket.Details["Instance Number"]}'");
            //        if (samplePacket.Details.ContainsKey("Property"))
            //            System.Diagnostics.Debug.WriteLine($"    Property: '{samplePacket.Details["Property"]}'");
            //        sampleCount++;
            //    }
            //}

            foreach (var packet in bacnetPackets)
            {
                if (packet.Details == null || packet.Details.Count == 0)
                    continue;

                debugCheckedPackets++;

                // Schritt 1: Prüfe ob es ein Confirmed-Request (PDU Type 0) ist
                var isConfirmedReq = false;
                if (packet.Details.TryGetValue("BACnet Type", out var typeValue) && !string.IsNullOrWhiteSpace(typeValue))
                {
                    // BACnet Type kann als Zahl oder Text kommen
                    // PDU Type 0 = Confirmed-REQ
                    if (typeValue == "0")
                    {
                        isConfirmedReq = true;
                    }
                    else
                    {
                        var typeLower = typeValue.ToLowerInvariant();
                        if (typeLower.Contains("confirmed-req") ||
                            (typeLower.Contains("confirmed") && typeLower.Contains("request")))
                        {
                            isConfirmedReq = true;
                        }
                    }
                }

                if (!isConfirmedReq)
                {
                    continue;
                }

                debugConfirmedReqCount++;

                // Schritt 2: Prüfe ServiceChoice - ReadProperty (12) oder ReadPropertyMultiple (14)
                var confirmedCode = GetServiceCode(packet.Details, "BACnet Confirmed Service Code", "BACnet Confirmed Service");
                int? serviceCode = confirmedCode;

                if (!serviceCode.HasValue)
                {
                    serviceCode = GetServiceCode(packet.Details, "BACnet Service Code", "BACnet Service");
                }

                var isReadProperty = serviceCode.HasValue && (serviceCode.Value == 12 || serviceCode.Value == 14);
                if (!isReadProperty && packet.Details.TryGetValue("BACnet Confirmed Service", out var confServiceText))
                {
                    var svcLower = (confServiceText ?? string.Empty).ToLowerInvariant();
                    if (svcLower.Contains("readpropertymultiple") || svcLower.Contains("read property multiple") || svcLower.Contains("read-property-multiple") ||
                        svcLower.Contains("readproperty") || svcLower.Contains("read property") || svcLower.Contains("read-property"))
                        isReadProperty = true;
                }

                if (!isReadProperty)
                {
                    continue;
                }

                debugReadPropServiceCount++;

                // ★ SCHRITT: Behandle ReadPropertyMultiple (ServiceChoice 14) mit mehreren Objekten
                // Diese MÜSSEN vor den normalen Object Type Filtern behandelt werden!
                // WICHTIG: Prüfe auf "Object Types (All)" statt BACnetObjectCount, da dieser zurückgesetzt wird!
                if (serviceCode == 14 &&
                    packet.Details.TryGetValue("Object Types (All)", out var objTypesAll) &&
                    packet.Details.TryGetValue("Instance Numbers (All)", out var instancesAll))
                {
                    //if (isSpecialCase)
                    //    System.Diagnostics.Debug.WriteLine($"[SPECIAL-TRACE] Paket #{packet.PacketNumber}: RPM Multi-Objekt Path gefunden (hat (All)-Felder)");

                    debugServiceCode14++;
                    debugRpmMultiObjectPackets++;

                    var types = objTypesAll.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                    var instances = instancesAll.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

                    debugRpmMultiObjectCount += Math.Max(types.Count, instances.Count) - 1;

                    int maxCount = Math.Max(types.Count, instances.Count);
                    if (maxCount > 0)
                    {
                        if (types.Count == 0)
                            types = Enumerable.Repeat("unbekannt", maxCount).ToList();
                        else if (types.Count == 1 && maxCount > 1)
                            types = Enumerable.Repeat(types[0], maxCount).ToList();

                        if (instances.Count == 0)
                            instances = Enumerable.Repeat("?", maxCount).ToList();
                        else if (instances.Count == 1 && maxCount > 1)
                            instances = Enumerable.Repeat(instances[0], maxCount).ToList();

                        string sourceIpMulti = string.IsNullOrWhiteSpace(packet.SourceIp) ? "Unbekannt" : packet.SourceIp;
                        string destIpMulti = string.IsNullOrWhiteSpace(packet.DestinationIp) ? "Unbekannt" : packet.DestinationIp;

                        //System.Diagnostics.Debug.WriteLine($"[Multi-RPM] Paket #{packet.PacketNumber}: {maxCount} Objekte");

                        for (int i = 0; i < Math.Min(types.Count, instances.Count); i++)
                        {
                            var objType = types[i];
                            var instNum = instances[i];

                            // Zähle ALLE Objekte für Gesamtstatistik
                            totalReadPropertyRequests++;

                            // Überspringe Device-Objekte (Type 8) nur für die Gruppierung
                            if (objType == "8")
                                continue;

                            totalReadPropertyCount++;
                            var multiKey = $"{sourceIpMulti} → {destIpMulti} | {objType},{instNum}";
                            if (!readPropertyGroups.ContainsKey(multiKey))
                                readPropertyGroups[multiKey] = 0;
                            readPropertyGroups[multiKey]++;
                        }

                        //if (isSpecialCase)
                        //    System.Diagnostics.Debug.WriteLine($"[SPECIAL-TRACE] Paket #{packet.PacketNumber}: RPM-Multi continue - überspringe restlichen Code");
                        continue; // Nächstes Paket!
                    }
                }

                // Zähle ServiceChoice 12 vs 14
                if (serviceCode == 12)
                    debugServiceCode12++;
                else if (serviceCode == 14)
                    debugServiceCode14++;

                // Zähle dieses Paket für Gesamtstatistik (wird nur einmal pro Paket gezählt)
                totalReadPropertyRequests++;

                // Schritt 3: Lese Object Type und Instance Number aus
                packet.Details.TryGetValue("Object Type", out var objectType);
                packet.Details.TryGetValue("Instance Number", out var instanceNumber);

                // Zähle wenn Object Type fehlt
                if (string.IsNullOrWhiteSpace(objectType))
                {
                    debugNoObjectTypeFound++;
                }

                // Schritt 4: Schließe Object Type 8 (device) aus
                if (!string.IsNullOrWhiteSpace(objectType))
                {
                    // Prüfe ob es Type 8 ist (numerisch oder als Text "device")
                    var objTypeLower = objectType.ToLowerInvariant();
                    if (objectType == "8" || objTypeLower.Contains("device"))
                    {
                        debugObjectType8Excluded++;
                        continue;
                    }
                }

                totalReadPropertyCount++;

                var sourceIp = string.IsNullOrWhiteSpace(packet.SourceIp) ? "Unbekannt" : packet.SourceIp;
                var destIp = string.IsNullOrWhiteSpace(packet.DestinationIp) ? "Unbekannt" : packet.DestinationIp;

                objectType = string.IsNullOrWhiteSpace(objectType) ? "unbekannt" : objectType;
                instanceNumber = string.IsNullOrWhiteSpace(instanceNumber) ? "?" : instanceNumber;

                // Schritt 5: Gruppiere nach Quelle-IP + Ziel-IP + Object Type + Instance
                // WICHTIG: Service Code (12 vs 14) wird NICHT berücksichtigt,
                // d.h. ReadProperty und ReadPropertyMultiple zur gleichen Instanz werden zusammengezählt
                var key = $"{sourceIp} → {destIp} | {objectType},{instanceNumber}";

                if (!readPropertyGroups.ContainsKey(key))
                    readPropertyGroups[key] = 0;
                readPropertyGroups[key]++;
            }

            var topReadProps = readPropertyGroups
                .Select(kv => new { Label = kv.Key, Count = kv.Value })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            //// Debug-Info: Zeige Filterung-Statistik
            System.Diagnostics.Debug.WriteLine($"");
            System.Diagnostics.Debug.WriteLine($"╔════════════════════════════════════════════════════════════════════════════╗");
            System.Diagnostics.Debug.WriteLine($"║         ReadProperty/ReadPropertyMultiple ANALYSE - STATISTIK             ║");
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ 1. PAKET-FILTERUNG                                                         ║");
            System.Diagnostics.Debug.WriteLine($"╟────────────────────────────────────────────────────────────────────────────╢");
            System.Diagnostics.Debug.WriteLine($"║   Gesamt BACnet-Pakete:              {bacnetPackets.Count,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   Geprüfte Pakete:                   {debugCheckedPackets,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   → Confirmed-Request (Type 0):      {debugConfirmedReqCount,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   → Mit ServiceChoice 12/14:         {debugReadPropServiceCount,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ 2. SERVICE-VERTEILUNG                                                      ║");
            System.Diagnostics.Debug.WriteLine($"╟────────────────────────────────────────────────────────────────────────────╢");
            System.Diagnostics.Debug.WriteLine($"║   ReadProperty (SC 12):              {debugServiceCode12,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   ReadPropertyMultiple (SC 14):      {debugServiceCode14,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ 3. MULTI-OBJEKT RPM EXTRAKTION                                             ║");
            System.Diagnostics.Debug.WriteLine($"╟────────────────────────────────────────────────────────────────────────────╢");
            System.Diagnostics.Debug.WriteLine($"║   RPM-Pakete mit >1 Objekt:          {debugRpmMultiObjectPackets,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   Zusätzliche Objekte extrahiert:    {debugRpmMultiObjectCount,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"╠════════════════════════════════════════════════════════════════════════════╣");
            System.Diagnostics.Debug.WriteLine($"║ 4. AUSSCHLÜSSE & FINALE ZÄHLUNG                                            ║");
            System.Diagnostics.Debug.WriteLine($"╟────────────────────────────────────────────────────────────────────────────╢");
            System.Diagnostics.Debug.WriteLine($"║   Pakete ohne Object Type:           {debugNoObjectTypeFound,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   Object Type 8 (Device) excluded:   {debugObjectType8Excluded,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   Finale Pakete gezählt:             {totalReadPropertyCount,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   GESAMT RP/RPM Requests (SC12+14):  {debugServiceCode12 + debugServiceCode14,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"║   Unique IP/Instanz Kombinationen:   {readPropertyGroups.Count,8}                           ║");
            System.Diagnostics.Debug.WriteLine($"╚════════════════════════════════════════════════════════════════════════════╝");
            System.Diagnostics.Debug.WriteLine($"");

            //// Debug: Zeige die TOP 10 Pakete
            //System.Diagnostics.Debug.WriteLine("=== TOP 10 ReadProperty Anfragen ===");
            //for (int i = 0; i < topReadProps.Count; i++)
            //{
            //    System.Diagnostics.Debug.WriteLine($"  {i + 1}. {topReadProps[i].Label} - {topReadProps[i].Count}x");
            //}
            //System.Diagnostics.Debug.WriteLine("====================================");

            if (topReadProps.Count == 0)
            {
                if (BACnetTopReadPropertyBorder != null)
                    BACnetTopReadPropertyBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (BACnetTopReadPropertyBorder != null)
                BACnetTopReadPropertyBorder.Visibility = Visibility.Visible;

            double durationSeconds = 1;
            if (filteredPackets.Count >= 2)
            {
                var minTs = filteredPackets.Min(p => p.Timestamp);
                var maxTs = filteredPackets.Max(p => p.Timestamp);
                durationSeconds = Math.Max(1e-6, (maxTs - minTs).TotalSeconds);
            }
            var durationMinutes = durationSeconds / 60.0;

            // Verwende die Summe der bereits gezählten Service Codes 12 + 14
            int totalRpRpmRequests = debugServiceCode12 + debugServiceCode14;
            var perMinute = durationMinutes > 0 ? totalRpRpmRequests / durationMinutes : 0;

            if (TopReadPropertyCountLabel != null)
                TopReadPropertyCountLabel.Text = string.Format(CultureInfo.GetCultureInfo("de-DE"), "{0} Total - {1:F2}/min", totalRpRpmRequests, perMinute);

            var topReadPropsWithRate = topReadProps.Select(x => new
            {
                x.Label,
                x.Count,
                Percentage = totalReadPropertyCount > 0 ? (x.Count * 100.0) / totalReadPropertyCount : 0.0,
                PerMinute = durationMinutes > 0 ? x.Count / durationMinutes : 0.0,
                DisplayValue = string.Format(CultureInfo.GetCultureInfo("de-DE"), "{0} ({1:F1}%)", x.Count, totalReadPropertyCount > 0 ? (x.Count * 100.0) / totalReadPropertyCount : 0.0)
            }).ToList();

            var barHeight = 22;
            var desiredHeight = Math.Max(10, topReadPropsWithRate.Count) * barHeight;

            var model = new PlotModel
            {
                Title = "Top ReadProperty/ReadPropertyMultiple Wiederholungen",
                Background = OxyColors.White
            };

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                ItemsSource = topReadPropsWithRate,
                LabelField = "Label",
                GapWidth = 0.5
            };
            model.Axes.Add(categoryAxis);

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Anzahl Anfragen",
                MinimumPadding = 0,
                AbsoluteMinimum = 0,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
                MinorGridlineColor = OxyColor.FromRgb(240, 240, 240)
            };
            model.Axes.Add(valueAxis);

            if (ReadPropertyTopChart != null)
            {
                ReadPropertyTopChart.Height = desiredHeight;
            }

            var series = new BarSeries
            {
                FillColor = OxyColor.FromRgb(111, 66, 193),
                StrokeColor = OxyColor.FromRgb(90, 54, 157),
                StrokeThickness = 1,
                BarWidth = 0.6,
                LabelPlacement = LabelPlacement.Inside,
                LabelMargin = 2,
                TextColor = OxyColors.White
            };

            model.Annotations.Clear();
            for (int i = 0; i < topReadPropsWithRate.Count; i++)
            {
                var item = topReadPropsWithRate[i];
                var barItem = new BarItem
                {
                    Value = item.Count,
                    CategoryIndex = i
                };
                series.Items.Add(barItem);

                var annotation = new OxyPlot.Annotations.TextAnnotation
                {
                    Text = item.DisplayValue,
                    TextPosition = new DataPoint(item.Count / 2.0, i),
                    TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                    TextVerticalAlignment = OxyPlot.VerticalAlignment.Middle,
                    TextColor = OxyColors.White,
                    Stroke = OxyColors.Transparent,
                    StrokeThickness = 0
                };
                model.Annotations.Add(annotation);
            }

            model.Series.Add(series);

            if (ReadPropertyTopChart != null)
            {
                ReadPropertyTopChart.Model = model;
                ReadPropertyTopChart.Controller = _noWheelController;
            }

            // Initialer Hinweistext (kompakt)
            if (TopReadPropertyInfoLabel != null)
            {
                TopReadPropertyInfoLabel.Text = "Bewegen Sie die Maus über einen Balken für Details zum Eintrag.";
            }
        }

        private void ReadPropertyTopChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (ReadPropertyTopChart?.Model == null)
                return;

            var model = ReadPropertyTopChart.Model;
            var categoryAxis = model.Axes.FirstOrDefault(a => a is CategoryAxis) as CategoryAxis;
            if (categoryAxis == null)
                return;

            var pos = e.GetPosition(ReadPropertyTopChart);
            var yData = categoryAxis.InverseTransform(pos.Y);
            var series = model.Series.OfType<BarSeries>().FirstOrDefault();
            if (series == null || series.Items.Count == 0)
                return;

            var index = (int)Math.Round(yData);
            index = Math.Max(0, Math.Min(series.Items.Count - 1, index));

            // Rekonstruiere die Datenquelle, die in CategoryAxis.ItemsSource verwendet wurde
            var itemsSource = (model.Axes.OfType<CategoryAxis>().FirstOrDefault()?.ItemsSource as System.Collections.IList);
            if (itemsSource == null || index >= itemsSource.Count)
                return;

            var itemObj = itemsSource[index];
            if (itemObj == null)
                return;

            dynamic item = itemObj;
            try
            {
                int count = (int)item.Count;
                double percentage = (double)item.Percentage;
                double perMinute = (double)item.PerMinute;
                string label = (string)item.Label;

                var info = $"Eintrag: {label}\n" +
                           $"Gesamt: {count}\n" +
                           $"Durchschnitt: {perMinute:F2}/Min\n" +
                           $"Rel. Verteilung: {percentage:F1}%";

                if (TopReadPropertyInfoLabel != null)
                    TopReadPropertyInfoLabel.Text = info;
            }
            catch
            {
                if (TopReadPropertyInfoLabel != null)
                    TopReadPropertyInfoLabel.Text = "Bewegen Sie die Maus über die Balken, um Details anzuzeigen...";
            }
        }

        private void ReadPropertyTopChart_MouseLeave(object sender, MouseEventArgs e)
        {
            if (TopReadPropertyInfoLabel != null)
            {
                TopReadPropertyInfoLabel.Text = "Bewegen Sie die Maus über einen Balken für Details zum Eintrag.";
            }
        }

        private void UpdateBACnetPacketsPerSecond(List<NetworkPacket> filteredPackets)
        {
            var bacnetPackets = filteredPackets.Where(p =>
                (p.ApplicationProtocol?.ToUpper() == "BACNET") ||
                (p.DestinationPort >= 47808 && p.DestinationPort <= 47823) ||
                (p.SourcePort >= 47808 && p.SourcePort <= 47823)).ToList();

            if (bacnetPackets.Count == 0)
            {
                if (BACnetPacketsPerSecBorder != null)
                    BACnetPacketsPerSecBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (BACnetPacketsPerSecBorder != null)
                BACnetPacketsPerSecBorder.Visibility = Visibility.Visible;

            var startTime = filteredPackets.Count > 0 ? filteredPackets.Min(p => p.Timestamp) : DateTime.Now;
            var endTime = filteredPackets.Count > 0 ? filteredPackets.Max(p => p.Timestamp) : DateTime.Now;

            // Berechne BACnet Packets/sec
            var packetsPerSecond = CalculatePacketsPerSecondForChart(bacnetPackets, startTime, endTime);

            // Erstelle das Plot-Modell
            var plotModel = new PlotModel
            {
                Title = "BACnet-Pakete pro Sekunde",
                Background = OxyColors.White
            };

            // Konfiguriere X-Achse (Zeit in Sekunden seit Start)
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Zeit (s)",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
                MinorGridlineColor = OxyColor.FromRgb(240, 240, 240),
                Minimum = 0,
                Maximum = Math.Max(0.0001, (endTime - startTime).TotalSeconds),
                IsPanEnabled = false,
                IsZoomEnabled = false
            };
            plotModel.Axes.Add(xAxis);

            // Konfiguriere Y-Achse (Pakete/Sekunde)
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Pakete/s",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.FromRgb(220, 220, 220),
                MinorGridlineColor = OxyColor.FromRgb(240, 240, 240),
                Minimum = 0,
                AbsoluteMinimum = 0,
                IsPanEnabled = false,
                IsZoomEnabled = false
            };
            plotModel.Axes.Add(yAxis);

            // Erstelle die Linienserie
            var lineSeries = new LineSeries
            {
                Title = "BACnet Pakete/s",
                Color = OxyColor.FromRgb(33, 150, 243),
                StrokeThickness = 2,
                MarkerType = MarkerType.None,
                CanTrackerInterpolatePoints = false
            };

            lineSeries.Points.AddRange(packetsPerSecond);

            plotModel.Series.Add(lineSeries);

            // Setze das Plot-Modell
            if (BACnetTimeChart != null)
            {
                BACnetTimeChart.Model = plotModel;
                BACnetTimeChart.Controller = _noWheelController;
            }
        }

        private List<DataPoint> CalculatePacketsPerSecondForChart(List<NetworkPacket> packets, DateTime startTime, DateTime endTime)
        {
            var points = new List<DataPoint>();

            if (packets.Count == 0)
            {
                points.Add(new DataPoint(0, 0));
                return points;
            }

            var duration = (endTime - startTime).TotalSeconds;
            if (duration < 1e-6)
            {
                points.Add(new DataPoint(0, packets.Count));
                return points;
            }

            var intervalSeconds = 1.0;
            var intervals = (int)Math.Ceiling(duration / intervalSeconds);

            // Zähle Pakete pro Intervall
            var countsPerInterval = new int[intervals];
            foreach (var packet in packets)
            {
                var offset = (packet.Timestamp - startTime).TotalSeconds;
                var index = (int)(offset / intervalSeconds);
                if (index >= 0 && index < intervals)
                    countsPerInterval[index]++;
            }

            // Erstelle DataPoints für das Chart
            for (int i = 0; i < intervals; i++)
            {
                var timeOffset = i * intervalSeconds;
                points.Add(new DataPoint(timeOffset, countsPerInterval[i]));
            }

            // Füge Endpunkt hinzu
            if (intervals > 0)
            {
                points.Add(new DataPoint(duration, countsPerInterval[intervals - 1]));
            }

            return points;
        }

        private void UpdateBACnetDatabaseStats()
        {
            if (BACnetDatabaseStatsBorder != null && _bacnetDb != null)
            {
                BACnetDatabaseStatsBorder.Visibility = Visibility.Visible;

                int totalDevices = _bacnetDb.AllDevices?.Count ?? 0;
                int withInstance = _bacnetDb.IpToInstance?.Count ?? 0;
                int withoutInstance = Math.Max(0, totalDevices - withInstance);
                int vendorDistinct = _bacnetDb.IpToVendorId?.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct()
                    .Count() ?? 0;

                if (BACnetDbTotalDevicesLabel != null)
                    BACnetDbTotalDevicesLabel.Text = totalDevices.ToString();
                if (BACnetDbNoInstanceCountLabel != null)
                    BACnetDbNoInstanceCountLabel.Text = withoutInstance.ToString();
                if (BACnetDbVendorDistinctLabel != null)
                    BACnetDbVendorDistinctLabel.Text = vendorDistinct.ToString();
            }
        }

        private static int? GetServiceCode(Dictionary<string, string> details, string codeKey, string valueKey)
        {
            if (details.TryGetValue(codeKey, out var codeStr))
            {
                if (int.TryParse(codeStr, out var parsedCode))
                    return parsedCode;
            }

            if (details.TryGetValue(valueKey, out var valueStr))
            {
                if (TryParseServiceCode(valueStr, out var parsedCode))
                    return parsedCode;
            }

            return null;
        }

        private static bool TryParseServiceCode(string? serviceValue, out int serviceCode)
        {
            serviceCode = -1;
            if (string.IsNullOrWhiteSpace(serviceValue))
                return false;

            if (int.TryParse(serviceValue.Trim(), out serviceCode))
                return true;

            var digits = new string(serviceValue.Where(char.IsDigit).ToArray());
            if (!string.IsNullOrEmpty(digits) && int.TryParse(digits, out serviceCode))
                return true;

            return false;
        }

    }
}
