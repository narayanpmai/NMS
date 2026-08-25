using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class ConfigurationBackup
    {
        public int Id { get; set; }
        
        public int DeviceId { get; set; }
        public virtual Device Device { get; set; }

        public string ConfigContent { get; set; }
        
        public DateTime BackupDate { get; set; } = DateTime.UtcNow;
        
        public bool IsCompliant { get; set; }
        
        public string ConfigVersion { get; set; }
    }
}
