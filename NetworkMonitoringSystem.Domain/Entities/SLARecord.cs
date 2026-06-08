using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class SLARecord
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public virtual Device? Device { get; set; }
        public DateTime Month { get; set; }
        public double UptimePercentage { get; set; }
        public double TargetPercentage { get; set; } = 99.9;
        public bool IsCompliant => UptimePercentage >= TargetPercentage;
    }
}
