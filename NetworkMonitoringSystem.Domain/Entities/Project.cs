using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NetworkMonitoringSystem.Domain.Common;

namespace NetworkMonitoringSystem.Domain.Entities
{
    public class Project : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        public string Description { get; set; }

        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
