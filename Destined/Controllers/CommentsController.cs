using Destined.Data;
using Destined.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Destined.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Ticket(int ticketId, string sort = "newest", Guid? seed = null)
        {
            var ticket = await _context.Tickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null || !ticket.IsPublic)
                return NotFound();

            var commentsQuery = _context.TicketComments
                .Where(c => c.TicketId == ticketId && c.ParentCommentId == null)
                .Include(c => c.User)
                .Include(c => c.Replies)
                .ThenInclude(r => r.User)
                .AsQueryable();

            List<TicketComment> comments;

            if (sort == "random" && TempData["Seed"] != null)
            {
                seed = (Guid)TempData["Seed"];
            }

            switch (sort)
            {
                case "oldest":
                    comments = await commentsQuery.OrderBy(c => c.CreatedOn).ToListAsync();
                    break;
                case "longest":
                    comments = await commentsQuery.OrderByDescending(c => c.Content.Length).ToListAsync();
                    break;
                case "random":
                    if (seed == null)
                        seed = Guid.NewGuid();

                    var commentsList = await commentsQuery.AsNoTracking().ToListAsync();
                    var rng = new Random(seed.Value.GetHashCode());
                    comments = commentsList.OrderBy(c => rng.Next()).ToList();
                    ViewBag.Seed = seed;
                    break;
                default:
                    comments = await commentsQuery.OrderByDescending(c => c.CreatedOn).ToListAsync();
                    break;
            }

            ViewBag.Ticket = ticket;
            ViewBag.Sort = sort;

            return View(comments);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int ticketId, string content, int? parentCommentId, string sort, Guid? seed)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || !ticket.IsPublic)
                return NotFound();

            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Ticket", new { ticketId, sort, seed });

            var currentUser = await _userManager.GetUserAsync(User);

            var comment = new TicketComment
            {
                TicketId = ticketId,
                Content = content,
                UserId = currentUser.Id,
                ParentCommentId = parentCommentId
            };

            _context.TicketComments.Add(comment);
            await _context.SaveChangesAsync();

            if (sort == "random" && seed != null)
                TempData["Seed"] = seed;

            return RedirectToAction("Ticket", new { ticketId, sort });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, string sort, Guid? seed)
        {
            var comment = await _context.TicketComments
                .Include(c => c.Ticket)
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comment == null)
                return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var currentUser = await _userManager.GetUserAsync(User);
            bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            bool canDelete =
                isAdmin ||
                currentUserId == comment.UserId ||
                currentUserId == comment.Ticket.UserId;

            if (!canDelete)
                return Unauthorized();

            await DeleteCommentRecursive(comment);

            await _context.SaveChangesAsync();

            if (sort == "random" && seed != null)
                TempData["Seed"] = seed;

            return RedirectToAction("Ticket", new { ticketId = comment.TicketId, sort });
        }

        private async Task DeleteCommentRecursive(TicketComment comment)
        {
            await _context.Entry(comment)
                .Collection(c => c.Replies)
                .LoadAsync();

            foreach (var reply in comment.Replies.ToList())
            {
                await DeleteCommentRecursive(reply);
            }

            _context.TicketComments.Remove(comment);
        }
    }
}
