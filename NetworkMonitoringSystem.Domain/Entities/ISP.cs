using System;
using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    /// <summary>
    /// Represents an Internet Service Provider circuit/link being monitored.
    /// </summary>
    public class ISP : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>Display name, e.g. "Primary ADSL Link"</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Provider brand, e.g. "WorldLink Communications"</summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>Circuit / Service ID issued by the ISP</summary>
        public string CircuitId { get; set; } = string.Empty;

        /// <summary>Contracted bandwidth in Mbps</summary>
        public double BandwidthMbps { get; set; }

        /// <summary>Connection type: Fiber / DSL / MPLS / Leased Line / LTE</summary>
        public string ConnectionType { get; set; } = "Fiber";

        /// <summary>Public gateway IP or ping target (e.g. 8.8.8.8 or the ISP's gateway)</summary>
        public string PingTarget { get; set; } = "8.8.8.8";

        /// <summary>Secondary target for cross-validation</summary>
        public string SecondaryPingTarget { get; set; } = "1.1.1.1";

        /// <summary>Current reachability status: Online / Degraded / Offline</summary>
        public string Status { get; set; } = "Unknown";

        /// <summary>Last measured round-trip latency in ms</summary>
        public double? LastLatencyMs { get; set; }

        /// <summary>Last measured packet loss percentage (0-100)</summary>
        public double? PacketLossPercent { get; set; }

        /// <summary>UTC timestamp of the last successful check</summary>
        public DateTime? LastCheckedAt { get; set; }

        /// <summary>SLA uptime target, e.g. 99.9</summary>
        public double SlaTargetPercent { get; set; } = 99.9;

        /// <summary>Number of times the ISP went offline in the current month</summary>
        public int DowntimeCount { get; set; } = 0;

        /// <summary>Total downtime minutes in current month</summary>
        public int TotalDowntimeMinutes { get; set; } = 0;

        public string ContactPerson { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;

        public bool IsMonitoringEnabled { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
    }
}
