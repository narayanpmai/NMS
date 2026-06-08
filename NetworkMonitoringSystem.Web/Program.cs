using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetworkMonitoringSystem.Infrastructure.Data;
using NetworkMonitoringSystem.Infrastructure.Identity;
using NetworkMonitoringSystem.Infrastructure.Services;
using NetworkMonitoringSystem.Web.Hubs;
using Serilog;
using Hangfire;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Setup SignalR
builder.Services.AddSignalR();

// Setup EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Setup Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register Services for DI
builder.Services.AddTransient<INotificationService, SignalRNotificationService>();
builder.Services.AddTransient<IPingService, PingService>();
builder.Services.AddTransient<IDiscoveryService, DiscoveryService>();
builder.Services.AddTransient<IReportService, ReportService>();
builder.Services.AddTransient<IISPMonitoringService, ISPMonitoringService>();

// Register Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// Setup Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrate and Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR Hub
app.MapHub<MonitoringHub>("/monitoringHub");

// Schedule Hangfire recurring job for ping checks
try
{
    var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<IPingService>(
        "device-ping-check",
        service => service.ProcessPingChecksAsync(CancellationToken.None),
        Cron.Minutely()
    );
    recurringJobManager.AddOrUpdate<IPingService>(
        "device-sla-calculation",
        service => service.CalculateMonthlySlaAsync(CancellationToken.None),
        Cron.Hourly()
    );
    recurringJobManager.AddOrUpdate<IISPMonitoringService>(
        "isp-connectivity-check",
        service => service.ProcessISPChecksAsync(CancellationToken.None),
        Cron.Minutely()
    );
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to schedule recurring Hangfire jobs.");
}

app.Run();
