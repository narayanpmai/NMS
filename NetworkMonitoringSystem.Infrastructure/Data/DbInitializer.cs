using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NetworkMonitoringSystem.Infrastructure.Identity;
using System;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roleNames = { "Super Admin", "Network Administrator", "Operator", "Viewer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create a default Super Admin user
            var adminUser = await userManager.FindByEmailAsync("admin@nms.local");
            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = "admin@nms.local",
                    Email = "admin@nms.local",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Super Admin");
                }
            }

            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (!dbContext.DeviceTypes.Any())
            {
                dbContext.DeviceTypes.AddRange(
                    new Domain.Entities.DeviceType { Name = "Router", Icon = "fa-network-wired" },
                    new Domain.Entities.DeviceType { Name = "Switch", Icon = "fa-server" },
                    new Domain.Entities.DeviceType { Name = "Firewall", Icon = "fa-shield-halved" },
                    new Domain.Entities.DeviceType { Name = "Server", Icon = "fa-server" }
                );
            }

            if (!dbContext.DeviceStatuses.Any())
            {
                dbContext.DeviceStatuses.AddRange(
                    new Domain.Entities.DeviceStatus { Name = "Online", ColorCode = "success" },
                    new Domain.Entities.DeviceStatus { Name = "Offline", ColorCode = "danger" },
                    new Domain.Entities.DeviceStatus { Name = "Warning", ColorCode = "warning" }
                );
            }

            if (!dbContext.AlertRules.Any())
            {
                dbContext.AlertRules.AddRange(
                    new Domain.Entities.AlertRule
                    {
                        Name = "High Latency Warning",
                        MetricType = "Latency",
                        Operator = ">",
                        ThresholdValue = 150.0,
                        Severity = "Warning",
                        IsEnabled = true
                    },
                    new Domain.Entities.AlertRule
                    {
                        Name = "Critical Latency Breach",
                        MetricType = "Latency",
                        Operator = ">",
                        ThresholdValue = 300.0,
                        Severity = "Critical",
                        IsEnabled = true
                    },
                    new Domain.Entities.AlertRule
                    {
                        Name = "High CPU Usage Warning",
                        MetricType = "CPU",
                        Operator = ">",
                        ThresholdValue = 80.0,
                        Severity = "Warning",
                        IsEnabled = true
                    },
                    new Domain.Entities.AlertRule
                    {
                        Name = "Critical Memory Usage Breach",
                        MetricType = "Memory",
                        Operator = ">",
                        ThresholdValue = 90.0,
                        Severity = "Critical",
                        IsEnabled = true
                    }
                );
            }

            if (!dbContext.Locations.Any())
            {
                dbContext.Locations.AddRange(
                    new Domain.Entities.Location { Name = "Kathmandu Headquarters", Latitude = 27.7172, Longitude = 85.3240, Address = "Kathmandu, Nepal" },
                    new Domain.Entities.Location { Name = "Lalitpur Municipal Office", Latitude = 27.6710, Longitude = 85.3218, Address = "Lalitpur, Nepal" },
                    new Domain.Entities.Location { Name = "Pokhara Branch", Latitude = 28.2096, Longitude = 83.9856, Address = "Pokhara, Nepal" },
                    new Domain.Entities.Location { Name = "Biratnagar Datacenter", Latitude = 26.4525, Longitude = 87.2718, Address = "Biratnagar, Nepal" },
                    new Domain.Entities.Location { Name = "Smart PanchPokhari", Latitude = 27.9711, Longitude = 85.7176, Address = "Panchpokhari, Sindhupalchok, Nepal" },
                    new Domain.Entities.Location { Name = "Auto Discovered Subnet", Latitude = 27.6866, Longitude = 85.3148, Address = "Dynamic Discovery" },
                    new Domain.Entities.Location { Name = "Server Room A, Rack 1", Latitude = 27.7007, Longitude = 85.3001, Address = "Building A, Kathmandu" },
                    new Domain.Entities.Location { Name = "Server Room B, Rack 2", Latitude = 27.7015, Longitude = 85.3020, Address = "Building B, Kathmandu" }
                );
            }
            
            await dbContext.SaveChangesAsync();
        }
    }
}
