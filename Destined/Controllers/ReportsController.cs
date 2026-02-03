using Destined.Data;
using Destined.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Destined.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var reports = await _context.TicketReports
                .Include(r => r.Ticket)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
            return View(reports);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.TicketReports.FindAsync(id);
            if (report != null)
            {
                _context.TicketReports.Remove(report);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                // Remove associated reports first? Or cascading delete?
                // Assuming EF handles cascade or we just remove ticket.
                // But wait, if we delete ticket, reports might hang if no cascade. 
                // Let's remove reports for this ticket first to be safe.
                var reports = _context.TicketReports.Where(r => r.TicketId == id);
                _context.TicketReports.RemoveRange(reports);

                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> CheckReportStatus(int ticketId)
        {
            var userId = _userManager.GetUserId(User);
            var exists = await _context.TicketReports
                .AnyAsync(r => r.TicketId == ticketId && r.ReporterId == userId);
            
            return Json(new { reported = exists });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(int ticketId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Length > 300)
            {
                // Validate length server side too
                TempData["Error"] = "Съобщението е твърде дълго или празно.";
                return RedirectToAction("PublicTickets", "Tickets"); 
            }

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var existingReport = await _context.TicketReports
                .FirstOrDefaultAsync(r => r.TicketId == ticketId && r.ReporterId == userId);

            if (existingReport != null)
            {
                TempData["Error"] = "Вече сте изпратили доклад за този билет!";
                return RedirectToAction("PublicTickets", "Tickets");
            }

            var report = new TicketReport
            {
                TicketId = ticketId,
                ReporterId = userId,
                Reason = reason,
                Timestamp = DateTime.Now
            };

            _context.TicketReports.Add(report);
            await _context.SaveChangesAsync();

            // Optionally add a tempdata message
            TempData["Message"] = "Билетът беше докладван успешно.";

            return RedirectToAction("PublicTickets", "Tickets");
        }
    }
}
