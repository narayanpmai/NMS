using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class DeviceType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } // e.g., Router, Switch, Server
        public string Icon { get; set; } // FontAwesome icon class
    }
}
