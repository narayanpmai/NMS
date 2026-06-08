using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class Device : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Hostname { get; set; }
        public string IPAddress { get; set; }
        public string MacAddress { get; set; }
        
        public int DeviceTypeId { get; set; }
        public virtual DeviceType DeviceType { get; set; }

        public string Vendor { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public string Department { get; set; }
        public string ContactPerson { get; set; }

        public int StatusId { get; set; }
        public virtual DeviceStatus Status { get; set; }

        public bool IsMonitoringEnabled { get; set; } = true;
        
        // Simulates whether the device has internet access allowed via network firewall/router
        public bool HasInternetAccess { get; set; } = true;

        public int? ProjectId { get; set; }
        public virtual Project Project { get; set; }
    }
}
