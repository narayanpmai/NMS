using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class ZtpProfile
    {
        public int Id { get; set; }
        
        public string MacAddress { get; set; }
        
        public int ZtpTemplateId { get; set; }
        public virtual ZtpTemplate Template { get; set; }
        
        // Stored as JSON, e.g. {"HOSTNAME": "Switch1", "IP_ADDRESS": "10.0.0.1"}
        public string VariablesJson { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
