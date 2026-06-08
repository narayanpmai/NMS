using NetworkMonitoringSystem.Domain.Common;
using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class Incident : AuditableEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, Assigned, In Progress, Resolved, Closed
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
        public int? DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public int? AlertId { get; set; }
        public virtual Alert? Alert { get; set; }
        public string? AssignedToUserId { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? RootCause { get; set; }
    }
}
