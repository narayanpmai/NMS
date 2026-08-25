using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class DeviceLink
    {
        public int Id { get; set; }
        
        public int SourceDeviceId { get; set; }
        public virtual Device SourceDevice { get; set; }

        public int TargetDeviceId { get; set; }
        public virtual Device TargetDevice { get; set; }

        public string SourcePort { get; set; } = "Unknown";
        public string TargetPort { get; set; } = "Unknown";

        public string LinkType { get; set; } = "Ethernet"; // e.g. Ethernet, Fiber, Wireless
        public long BandwidthMbps { get; set; } = 1000; // 1 Gbps default

        public bool IsActive { get; set; } = true;
        
        public DateTime LastDiscoveredAt { get; set; } = DateTime.UtcNow;
    }
}
