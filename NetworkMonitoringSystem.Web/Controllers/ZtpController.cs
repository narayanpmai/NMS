using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetworkMonitoringSystem.Domain.Entities;
using NetworkMonitoringSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace NetworkMonitoringSystem.Web.Controllers
{
    [Authorize]
    public class ZtpController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ZtpController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var templates = await _context.ZtpTemplates.ToListAsync();
            var profiles = await _context.ZtpProfiles.Include(p => p.Template).ToListAsync();
            
            ViewBag.Templates = templates;
            ViewBag.Profiles = profiles;
            
            return View();
        }

        public IActionResult CreateTemplate()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTemplate([Bind("Name,Description,Content")] ZtpTemplate template)
        {
            if (ModelState.IsValid)
            {
                _context.Add(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ZTP Template created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(template);
        }

        public async Task<IActionResult> CreateProfile()
        {
            ViewBag.Templates = new SelectList(await _context.ZtpTemplates.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile([Bind("MacAddress,ZtpTemplateId,VariablesJson")] ZtpProfile profile)
        {
            ModelState.Remove("Template"); // Don't validate the nav property
            if (ModelState.IsValid)
            {
                _context.Add(profile);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "ZTP Profile created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Templates = new SelectList(await _context.ZtpTemplates.ToListAsync(), "Id", "Name", profile.ZtpTemplateId);
            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var profile = await _context.ZtpProfiles.FindAsync(id);
            if (profile != null)
            {
                _context.ZtpProfiles.Remove(profile);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile deleted.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _context.ZtpTemplates.FindAsync(id);
            if (template != null)
            {
                if (await _context.ZtpProfiles.AnyAsync(p => p.ZtpTemplateId == id))
                {
                    TempData["ErrorMessage"] = "Cannot delete template because it is in use by one or more profiles.";
                }
                else
                {
                    _context.ZtpTemplates.Remove(template);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Template deleted.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
