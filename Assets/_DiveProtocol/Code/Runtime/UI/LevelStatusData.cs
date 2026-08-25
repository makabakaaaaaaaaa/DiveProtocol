namespace DiveProtocol.UI
{
    /// <summary>Immutable sector and threat-status information displayed by the gameplay CRT HUD.</summary>
    public sealed class LevelStatusData
    {
        public enum AlertLevel
        {
            Low,
            Medium,
            High,
            Critical
        }

        public LevelStatusData(string sceneName, string sectorCode, string areaName, AlertLevel alertLevel)
        {
            SceneName = sceneName ?? string.Empty;
            SectorCode = sectorCode ?? "--";
            AreaName = areaName ?? "FACILITY ACCESS";
            CurrentAlertLevel = alertLevel;
        }

        public string SceneName { get; }
        public string SectorCode { get; }
        public string AreaName { get; }
        public AlertLevel CurrentAlertLevel { get; }

        /// <summary>Formats this data using the fixed terminal vocabulary consumed by the HUD.</summary>
        public string ToHudText()
        {
            return $"SECTOR {SectorCode}\n{AreaName}\n\nALERT LEVEL\n{CurrentAlertLevel.ToString().ToUpperInvariant()}";
        }
    }
}
