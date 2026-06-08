using System.Collections.Generic;

namespace NetworkMonitoringSystem.Web.Models
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set; }
        public bool IsActive { get; set; }
    }
}
