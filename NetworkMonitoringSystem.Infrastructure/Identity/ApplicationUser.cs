using Microsoft.AspNetCore.Identity;

namespace NetworkMonitoringSystem.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
