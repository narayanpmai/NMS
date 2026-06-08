using NetworkMonitoringSystem.Domain.Common;
using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class Alert : AuditableEntity
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Level { get; set; } = "Warning"; // Critical, Warning, Info
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedAt { get; set; }
    }
}
