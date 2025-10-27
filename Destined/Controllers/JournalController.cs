using Destined.Data;
using Destined.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Destined.Controllers
{
    [Authorize]
    public class JournalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public JournalController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Page(int ticketId, int page = 1, bool readOnly = false)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
                return NotFound();

            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!ticket.IsPublic && ticket.UserId != currentUserId)
                return NotFound();

            if (ticket.IsPublic && readOnly == false && ticket.UserId != currentUserId)
                return Forbid();

            var journalPage = await _context.JournalPages
                .FirstOrDefaultAsync(p => p.TicketId == ticketId && p.PageNumber == page);

            if (journalPage == null)
            {
                if (!readOnly)
                {
                    journalPage = new JournalPage
                    {
                        TicketId = ticketId,
                        PageNumber = page,
                        Content = ""
                    };
                    _context.JournalPages.Add(journalPage);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    journalPage = new JournalPage
                    {
                        TicketId = ticketId,
                        PageNumber = page,
                        Content = ""
                    };
                }
            }

            var nextPage = await _context.JournalPages
                .FirstOrDefaultAsync(p => p.TicketId == ticketId && p.PageNumber == page + 1);

            bool hasContent = !string.IsNullOrWhiteSpace(journalPage.Content);
            bool hasNextPage = hasContent || (nextPage != null && !string.IsNullOrWhiteSpace(nextPage.Content));
            journalPage.HasNextPage = hasNextPage;

            ViewBag.TicketId = ticketId;
            ViewBag.ReadOnly = readOnly;

            return View(journalPage);
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return Json(new { location = "" });

            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var imageUrl = "/uploads/" + fileName;
            return Json(new { location = imageUrl });
        }

        [HttpPost]
        public async Task<IActionResult> SavePage(JournalPage model, IFormFile? image)
        {
            if (Request.Query.ContainsKey("readOnly") && Request.Query["readOnly"] == "true")
                return Forbid();

            var page = await _context.JournalPages
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (page == null)
                return NotFound();

            page.Content = model.Content;

            if (image != null && image.Length > 0)
            {
                var imagePath = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(image.FileName));
                var fullPath = Path.Combine(_env.WebRootPath, imagePath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                page.ImagePath = "/" + imagePath.Replace("\\", "/");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Page", new { ticketId = page.TicketId, page = page.PageNumber });
        }
    }
}
