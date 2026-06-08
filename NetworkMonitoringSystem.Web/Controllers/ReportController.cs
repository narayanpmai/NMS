using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Application.Interfaces;
using NetworkMonitoringSystem.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ApplicationDbContext _context;

        public ReportController(IReportService reportService, ApplicationDbContext context)
        {
            _reportService = reportService;
            _context = context;
        }

        // GET: Report
        public async Task<IActionResult> Index()
        {
            var devices = await _context.Devices.ToListAsync();
            ViewBag.Devices = new SelectList(devices, "Id", "Name");
            return View();
        }

        // GET: Report/DownloadSlaReport
        [HttpGet]
        public async Task<IActionResult> DownloadSlaReport(int deviceId, int year, int month)
        {
            try
            {
                var fileBytes = await _reportService.GenerateSlaReportPdfAsync(deviceId, year, month);
                var fileName = $"SlaReport_Device_{deviceId}_{year}_{month:D2}.pdf";
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate SLA PDF report: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Report/DownloadCapacityReport
        [HttpGet]
        public async Task<IActionResult> DownloadCapacityReport()
        {
            try
            {
                var fileBytes = await _reportService.GenerateCapacityReportExcelAsync();
                var fileName = $"CapacityReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate Capacity report: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
