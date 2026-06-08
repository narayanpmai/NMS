using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class AlertRule : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty; // CPU, Memory, Disk, Latency, Bandwidth
        public string Operator { get; set; } = ">"; // >, <, ==, >=, <=
        public double ThresholdValue { get; set; }
        public string Severity { get; set; } = "Warning"; // Critical, Warning, Info
        public bool IsEnabled { get; set; } = true;
    }
}
