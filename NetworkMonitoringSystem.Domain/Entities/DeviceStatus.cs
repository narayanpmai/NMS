using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class DeviceStatus : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., Online, Offline, Warning, Critical
        public string ColorCode { get; set; } // Hex color or Bootstrap class
    }
}
