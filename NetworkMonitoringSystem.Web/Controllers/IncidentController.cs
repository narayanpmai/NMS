using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using NetworkMonitoringSystem.Infrastructure.Identity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class IncidentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IncidentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Incident
        public async Task<IActionResult> Index(string statusFilter = "Active")
        {
            var query = _context.Incidents
                .Include(i => i.Device)
                .Include(i => i.Alert)
                .AsQueryable();

            if (statusFilter == "Active")
            {
                query = query.Where(i => i.Status == "Open" || i.Status == "Assigned" || i.Status == "In Progress");
            }
            else if (statusFilter != "All")
            {
                query = query.Where(i => i.Status == statusFilter);
            }

            var incidents = await query
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Populate users list for assignment dropdowns
            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = users;
            ViewBag.StatusFilter = statusFilter;

            return View(incidents);
        }

        // POST: Incident/AssignToSelf
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToSelf(int incidentId)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Admin";

            incident.AssignedToUserId = userId;
            incident.Status = "Assigned";
            
            _context.Update(incident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Incident assigned to you ({userName}).";
            return RedirectToAction(nameof(Index), new { statusFilter = ViewBag.StatusFilter });
        }

        // POST: Incident/AssignToUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToUser(int incidentId, string userId)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            incident.AssignedToUserId = userId;
            incident.Status = "Assigned";

            _context.Update(incident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Incident assigned to {user.FullName} ({user.Email}).";
            return RedirectToAction(nameof(Index));
        }

        // POST: Incident/Resolve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int incidentId, string rootCause)
        {
            var incident = await _context.Incidents
                .Include(i => i.Alert)
                .FirstOrDefaultAsync(i => i.Id == incidentId);

            if (incident == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(rootCause))
            {
                TempData["ErrorMessage"] = "Root Cause or resolution details must be provided.";
                return RedirectToAction(nameof(Index));
            }

            incident.Status = "Resolved";
            incident.ResolvedAt = DateTime.UtcNow;
            incident.RootCause = rootCause;

            if (incident.Alert != null)
            {
                incident.Alert.IsResolved = true;
                incident.Alert.ResolvedAt = DateTime.UtcNow;
            }

            _context.Update(incident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Incident ticket resolved successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Incident/Close
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(int incidentId)
        {
            var incident = await _context.Incidents.FindAsync(incidentId);
            if (incident == null)
            {
                return NotFound();
            }

            incident.Status = "Closed";

            _context.Update(incident);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Incident ticket closed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
