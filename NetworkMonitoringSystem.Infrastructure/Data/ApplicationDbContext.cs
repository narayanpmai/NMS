using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Domain.Common;
using NetworkMonitoringSystem.Infrastructure.Identity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // DbSets will be added here
        public DbSet<NetworkMonitoringSystem.Domain.Entities.Device> Devices { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.DeviceType> DeviceTypes { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.DeviceStatus> DeviceStatuses { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.Alert> Alerts { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.DeviceMetric> DeviceMetrics { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.AlertRule> AlertRules { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.Incident> Incidents { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.Location> Locations { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.SLARecord> SlaRecords { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.AuditLog> AuditLogs { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.ISP> ISPs { get; set; }
        public DbSet<NetworkMonitoringSystem.Domain.Entities.Project> Projects { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.CreatedBy = entry.Entity.CreatedBy ?? "System";
                        entry.Entity.LastModifiedAt = DateTime.UtcNow;
                        entry.Entity.LastModifiedBy = entry.Entity.LastModifiedBy ?? "System";
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = DateTime.UtcNow;
                        entry.Entity.LastModifiedBy = entry.Entity.LastModifiedBy ?? "System";
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
