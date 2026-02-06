namespace bacneTPana.Models
{
    /// <summary>
    /// Konfiguration für COV (Change of Value) Notification und ReadProperty Schwellwerte
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
        /// Obere Grenze für grüne Ampel (Gut) - ReadProperty - Standard: 6
        /// </summary>
        public int ReadPropertyGreenThreshold { get; set; } = 6;

        /// <summary>
        /// Obere Grenze für gelbe Ampel (OK) - ReadProperty - Standard: 30
        /// </summary>
        public int ReadPropertyYellowThreshold { get; set; } = 30;

        /// <summary>
        /// Obere Grenze für orange Ampel (Kritisch) - ReadProperty - Standard: 60
        /// </summary>
        public int ReadPropertyRedThreshold { get; set; } = 60;

        /// <summary>
        /// Farbe für grüne Ampel (Gut) - Standard: #28A745
        /// </summary>
        public string GreenColor { get; set; } = "#28A745";

        /// <summary>
        /// Farbe für gelbe Ampel (OK) - Standard: LightBlue
        /// </summary>
        public string YellowColor { get; set; } = "LightBlue";

        /// <summary>
        /// Farbe für orange Ampel (Schlecht) - Standard: #FF9800
        /// </summary>
        public string RedColor { get; set; } = "#FF9800";

        /// <summary>
        /// Farbe für rote Ampel (Kritisch) - Standard: #C62828
        /// </summary>
        public string CriticalColor { get; set; } = "#C62828";

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
        /// Bestimmt die Ampel-Bewertung für ReadProperty basierend auf dem Durchschnittswert pro Minute
        /// </summary>
        /// <param name="averagePerMinute">Durchschnittliche ReadProperty Anfragen pro Minute</param>
        /// <returns>Ampel-Status</returns>
        public TrafficLightStatus GetReadPropertyStatus(double averagePerMinute)
        {
            if (averagePerMinute <= ReadPropertyGreenThreshold)
                return TrafficLightStatus.Green;
            else if (averagePerMinute <= ReadPropertyYellowThreshold)
                return TrafficLightStatus.Yellow;
            else if (averagePerMinute <= ReadPropertyRedThreshold)
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
        /// Gibt die Farbe für die Ampel basierend auf dem Status zurück (statische Version mit Standardfarben)
        /// </summary>
        public static string GetStatusColor(TrafficLightStatus status)
        {
            return status switch
            {
                TrafficLightStatus.Green => "#28A745",    // Grün
                TrafficLightStatus.Yellow => "LightBlue",   // LightBlue
                TrafficLightStatus.Red => "#FF9800",       // Orange
                TrafficLightStatus.Critical => "#C62828",  // Rot (Kritisch)
                _ => "#666666"                             // Grau
            };
        }

        /// <summary>
        /// Gibt die Farbe für die Ampel basierend auf dem Status zurück (verwendet konfigurierte Farben)
        /// </summary>
        public string GetStatusColorFromConfig(TrafficLightStatus status)
        {
            return status switch
            {
                TrafficLightStatus.Green => GreenColor,
                TrafficLightStatus.Yellow => YellowColor,
                TrafficLightStatus.Red => RedColor,
                TrafficLightStatus.Critical => CriticalColor,
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
