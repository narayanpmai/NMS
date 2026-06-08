using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class ISPController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ISPController(ApplicationDbContext db) => _db = db;

        // GET /ISP
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "ISP Status";

            // Auto-seed two demo ISPs if the table is empty
            if (!await _db.ISPs.AnyAsync())
            {
                _db.ISPs.AddRange(
                    new ISP
                    {
                        Name = "Primary WAN - WorldLink",
                        Provider = "WorldLink Communications",
                        CircuitId = "WL-2026-001",
                        BandwidthMbps = 100,
                        ConnectionType = "Fiber",
                        PingTarget = "8.8.8.8",
                        SecondaryPingTarget = "1.1.1.1",
                        Status = "Unknown",
                        SlaTargetPercent = 99.9,
                        ContactPerson = "ISP NOC",
                        ContactPhone = "+977-1-4101111",
                        ContactEmail = "noc@worldlink.com.np",
                        Notes = "Primary internet circuit - office building"
                    },
                    new ISP
                    {
                        Name = "Backup LTE - NTC",
                        Provider = "Nepal Telecom",
                        CircuitId = "NTC-LTE-4G-002",
                        BandwidthMbps = 20,
                        ConnectionType = "LTE",
                        PingTarget = "1.1.1.1",
                        SecondaryPingTarget = "8.8.4.4",
                        Status = "Unknown",
                        SlaTargetPercent = 99.0,
                        ContactPerson = "NTC Helpdesk",
                        ContactPhone = "+977-1-4272720",
                        ContactEmail = "support@ntc.net.np",
                        Notes = "Failover LTE backup link"
                    }
                );
                await _db.SaveChangesAsync();
            }

            var isps = await _db.ISPs.OrderBy(i => i.Id).ToListAsync();
            return View(isps);
        }

        // GET /ISP/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Add ISP";
            return View(new ISP());
        }

        // POST /ISP/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ISP model)
        {
            ModelState.Remove(nameof(ISP.CreatedBy));
            ModelState.Remove(nameof(ISP.LastModifiedBy));

            if (ModelState.IsValid)
            {
                model.Status = "Unknown";
                _db.ISPs.Add(model);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"ISP '{model.Name}' added. Monitoring will begin within 1 minute.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Add ISP";
            return View(model);
        }

        // GET /ISP/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var isp = await _db.ISPs.FindAsync(id);
            if (isp == null) return NotFound();
            ViewData["Title"] = "Edit ISP";
            return View(isp);
        }

        // POST /ISP/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ISP model)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove(nameof(ISP.CreatedBy));
            ModelState.Remove(nameof(ISP.LastModifiedBy));

            if (ModelState.IsValid)
            {
                var existing = await _db.ISPs.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Name                = model.Name;
                existing.Provider            = model.Provider;
                existing.CircuitId           = model.CircuitId;
                existing.BandwidthMbps       = model.BandwidthMbps;
                existing.ConnectionType      = model.ConnectionType;
                existing.PingTarget          = model.PingTarget;
                existing.SecondaryPingTarget = model.SecondaryPingTarget;
                existing.SlaTargetPercent    = model.SlaTargetPercent;
                existing.ContactPerson       = model.ContactPerson;
                existing.ContactPhone        = model.ContactPhone;
                existing.ContactEmail        = model.ContactEmail;
                existing.Notes               = model.Notes;
                existing.IsMonitoringEnabled = model.IsMonitoringEnabled;

                _db.Update(existing);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"ISP '{existing.Name}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["Title"] = "Edit ISP";
            return View(model);
        }

        // POST /ISP/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var isp = await _db.ISPs.FindAsync(id);
            if (isp != null)
            {
                _db.ISPs.Remove(isp);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"ISP '{isp.Name}' removed.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET /ISP/GetStatusData  (JSON for dashboard widget)
        [HttpGet]
        public async Task<IActionResult> GetStatusData()
        {
            var isps = await _db.ISPs
                .Where(i => i.IsMonitoringEnabled)
                .Select(i => new
                {
                    i.Id, i.Name, i.Provider, i.Status,
                    i.LastLatencyMs, i.PacketLossPercent,
                    i.BandwidthMbps, i.ConnectionType,
                    checkedAgo = i.LastCheckedAt.HasValue
                        ? (int)(DateTime.UtcNow - i.LastCheckedAt.Value).TotalSeconds
                        : (int?)null
                })
                .ToListAsync();

            return Json(isps);
        }
    }
}
