using System;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class ZtpTemplate
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        public string Content { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
