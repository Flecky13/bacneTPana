namespace bacneTPana.Models
{
    /// <summary>
    /// Konfiguration für COV (Change of Value) Notification Schwellwerte
    /// </summary>
    public class COVThresholdConfig
    {
        /// <summary>
        /// Obere Grenze für grüne Ampel (Gut) - Standard: 2
        /// </summary>
        public int GreenThreshold { get; set; } = 2;

        /// <summary>
        /// Obere Grenze für gelbe Ampel (OK) - Standard: 10
        /// </summary>
        public int YellowThreshold { get; set; } = 10;

        /// <summary>
        /// Obere Grenze für orange Ampel (Schlecht) - Standard: 30
        /// </summary>
        public int RedThreshold { get; set; } = 30;

        /// <summary>
        /// Bestimmt die Ampel-Bewertung basierend auf dem Durchschnittswert pro Minute
        /// </summary>
        /// <param name="averagePerMinute">Durchschnittliche COV Notifications pro Minute</param>
        /// <returns>Ampel-Status</returns>
        public TrafficLightStatus GetStatus(double averagePerMinute)
        {
            if (averagePerMinute <= GreenThreshold)
                return TrafficLightStatus.Green;
            else if (averagePerMinute <= YellowThreshold)
                return TrafficLightStatus.Yellow;
            else if (averagePerMinute <= RedThreshold)
                return TrafficLightStatus.Red;
            else
                return TrafficLightStatus.Critical;
        }

        /// <summary>
        /// Gibt das Ampel-Symbol basierend auf dem Status zurück
        /// </summary>
        public static string GetStatusEmoji(TrafficLightStatus status)
        {
            return status switch
            {
                TrafficLightStatus.Green => "🟢",
                TrafficLightStatus.Yellow => "🟡",
                TrafficLightStatus.Red => "🟠",
                TrafficLightStatus.Critical => "🔴",
                _ => "⚪"
            };
        }

        /// <summary>
        /// Gibt die Farbe für die Ampel basierend auf dem Status zurück
        /// </summary>
        public static string GetStatusColor(TrafficLightStatus status)
        {
            return status switch
            {
                TrafficLightStatus.Green => "#28A745",    // Grün
                TrafficLightStatus.Yellow => "#FFC107",   // Gelb
                TrafficLightStatus.Red => "#FF9800",       // Orange
                TrafficLightStatus.Critical => "#C62828",  // Rot (Kritisch)
                _ => "#666666"                             // Grau
            };
        }

        /// <summary>
        /// Gibt den Bewertungstext basierend auf dem Status zurück
        /// </summary>
        public static string GetStatusText(TrafficLightStatus status)
        {
            return status switch
            {
                TrafficLightStatus.Green => "Gut",
                TrafficLightStatus.Yellow => "OK",
                TrafficLightStatus.Red => "Schlecht",
                TrafficLightStatus.Critical => "Kritisch",
                _ => "Unbekannt"
            };
        }
    }

    /// <summary>
    /// Ampel-Status für COV Notifications
    /// </summary>
    public enum TrafficLightStatus
    {
        /// <summary>
        /// 🟢 Gut (0-2 pro Minute)
        /// </summary>
        Green,

        /// <summary>
        /// 🟡 OK (3-10 pro Minute)
        /// </summary>
        Yellow,

        /// <summary>
        /// 🟠 Schlecht (>10 pro Minute)
        /// </summary>
        Red,

        /// <summary>
        /// 🔴 Kritisch (>30 pro Minute)
        /// </summary>
        Critical
    }
}
