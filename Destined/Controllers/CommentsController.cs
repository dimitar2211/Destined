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
        public async Task<IActionResult> Ticket(int ticketId, string sort = "newest")
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

            switch (sort)
            {
                case "oldest":
                    commentsQuery = commentsQuery.OrderBy(c => c.CreatedOn);
                    break;

                case "longest":
                    commentsQuery = commentsQuery.OrderByDescending(c => c.Content.Length);
                    break;

                case "random":
                    commentsQuery = commentsQuery.OrderBy(c => Guid.NewGuid());
                    break;

                default:
                    commentsQuery = commentsQuery.OrderByDescending(c => c.CreatedOn);
                    break;
            }

            var comments = await commentsQuery.ToListAsync();

            ViewBag.Ticket = ticket;
            ViewBag.Sort = sort;

            return View(comments);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int ticketId, string content, int? parentCommentId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null || !ticket.IsPublic)
                return NotFound();

            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction("Ticket", new { ticketId });

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

            return RedirectToAction("Ticket", new { ticketId });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
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

            return RedirectToAction("Ticket", new { ticketId = comment.TicketId });
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
