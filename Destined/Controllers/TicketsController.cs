using Destined.Data;
using Destined.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Destined.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TicketsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.OrderIndex)
                .ToListAsync();

            return View(tickets);
        }


        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (!ticket.IsPublic && ticket.UserId != currentUserId) return NotFound();

            if (!string.IsNullOrEmpty(currentUserId))
            {
                var isBlocked = await _context.BlockedUsers.AnyAsync(b => 
                    (b.BlockerId == currentUserId && b.BlockedId == ticket.UserId) || 
                    (b.BlockerId == ticket.UserId && b.BlockedId == currentUserId));
                if (isBlocked) return NotFound();
            }

            return View(ticket);
        }

        // GET: Tickets/Create
        public IActionResult Create() => View();

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("From,To,DepartureTime,NumberOfPassengers,LeftColor,RightColor,TextColor,Country")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

                ticket.UserId = currentUserId;
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.UserId != _userManager.GetUserId(User)) return NotFound();

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,From,To,DepartureTime,NumberOfPassengers,IsPublic,AllowComments,LeftColor,RightColor,TextColor,Country,OrderIndex")] Ticket ticket)
        {
            if (id != ticket.Id) return NotFound();

            var existingTicket = await _context.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (existingTicket == null || existingTicket.UserId != _userManager.GetUserId(User)) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    ticket.UserId = existingTicket.UserId;
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets.FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null || ticket.UserId != _userManager.GetUserId(User)) return NotFound();

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.UserId != _userManager.GetUserId(User)) return NotFound();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicTickets(string? searchCountry, string? searchTo, string? searchUser)
        {
            var query = _context.Tickets
                .Where(t => t.IsPublic)
                .Include(t => t.User)
                .AsQueryable();

            var currentUserId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var blockedByMe = _context.BlockedUsers.Where(b => b.BlockerId == currentUserId).Select(b => b.BlockedId);
                var blockedMe = _context.BlockedUsers.Where(b => b.BlockedId == currentUserId).Select(b => b.BlockerId);
                query = query.Where(t => !blockedByMe.Contains(t.UserId) && !blockedMe.Contains(t.UserId));
            }

            if (!string.IsNullOrEmpty(searchCountry))
            {
                query = query.Where(t => t.Country.Contains(searchCountry));
            }

            if (!string.IsNullOrEmpty(searchTo))
            {
                query = query.Where(t => t.To.Contains(searchTo));
            }

            if (!string.IsNullOrEmpty(searchUser))
            {
                // Find users where display_username claim or actual username matches
                var matchedUserIds = await _context.UserClaims
                    .Where(c => c.ClaimType == "display_username" && c.ClaimValue.Contains(searchUser))
                    .Select(c => c.UserId)
                    .ToListAsync();

                var matchedUserIdsByUsername = await _userManager.Users
                    .Where(u => u.UserName.Contains(searchUser))
                    .Select(u => u.Id)
                    .ToListAsync();

                var allMatchedIds = matchedUserIds.Union(matchedUserIdsByUsername).ToList();

                query = query.Where(t => allMatchedIds.Contains(t.UserId));
            }

            var publicTickets = await query
                .OrderBy(t => Guid.NewGuid())
                .ToListAsync();

            // Build userId → display username map
            var userIds = publicTickets
                .Where(t => t.UserId != null)
                .Select(t => t.UserId!)
                .Distinct()
                .ToList();

            var userDisplayNames = new Dictionary<string, string>();
            foreach (var uid in userIds)
            {
                var u = await _userManager.FindByIdAsync(uid);
                if (u != null)
                {
                    var claims = await _userManager.GetClaimsAsync(u);
                    var displayClaim = claims.FirstOrDefault(c => c.Type == "display_username");
                    if (displayClaim != null && !string.IsNullOrEmpty(displayClaim.Value))
                    {
                        userDisplayNames[uid] = displayClaim.Value;
                    }
                    else
                    {
                        var name = u.UserName ?? string.Empty;
                        var atIdx = name.IndexOf('@');
                        userDisplayNames[uid] = atIdx >= 0 ? name.Substring(0, atIdx) : name;
                    }
                }
            }

            // Build comment counts map so the view can hide the button when 0 comments
            var ticketIds = publicTickets.Select(t => t.Id).ToList();
            var commentCounts = await _context.TicketComments
                .Where(c => ticketIds.Contains(c.TicketId))
                .GroupBy(c => c.TicketId)
                .Select(g => new { TicketId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TicketId, x => x.Count);

            ViewData["SearchCountry"] = searchCountry;
            ViewData["SearchTo"] = searchTo;
            ViewData["SearchUser"] = searchUser;
            ViewData["UserDisplayNames"] = userDisplayNames;
            ViewData["CommentCounts"] = commentCounts;

            // Fetch Friend IDs and Pending Request IDs for UI logic
            var friendIds = new List<string>();
            var pendingRequestIds = new List<string>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                friendIds = await _context.Friendships
                    .Where(f => f.UserId == currentUserId)
                    .Select(f => f.FriendId)
                    .ToListAsync();

                pendingRequestIds = await _context.FriendRequests
                    .Where(r => r.Status == FriendRequestStatus.Pending && 
                                (r.SenderId == currentUserId || r.ReceiverId == currentUserId))
                    .Select(r => r.SenderId == currentUserId ? r.ReceiverId : r.SenderId)
                    .ToListAsync();
            }
            ViewData["FriendIds"] = friendIds;
            ViewData["PendingRequestIds"] = pendingRequestIds;
            ViewData["CurrentUserId"] = currentUserId;

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminUserIds = adminUsers.Select(u => u.Id).ToList();
            ViewData["AdminUserIds"] = adminUserIds;

            var likedTicketIds = new List<int>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                likedTicketIds = await _context.LikedTickets
                    .Where(lt => lt.UserId == currentUserId)
                    .Select(lt => lt.TicketId)
                    .ToListAsync();
            }
            ViewData["LikedTicketIds"] = likedTicketIds;

            return View(publicTickets);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int ticketId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var likedTicket = await _context.LikedTickets
                .FirstOrDefaultAsync(lt => lt.TicketId == ticketId && lt.UserId == userId);

            if (likedTicket != null)
            {
                _context.LikedTickets.Remove(likedTicket);
                await _context.SaveChangesAsync();
                return Json(new { liked = false });
            }
            else
            {
                var newLike = new LikedTicket
                {
                    TicketId = ticketId,
                    UserId = userId,
                    LikedAt = DateTime.UtcNow
                };
                _context.LikedTickets.Add(newLike);
                await _context.SaveChangesAsync();
                return Json(new { liked = true });
            }
        }

        public async Task<IActionResult> LikedTickets()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account", new { area = "Identity" });

            var likedTickets = await _context.LikedTickets
                .Where(lt => lt.UserId == userId)
                .Include(lt => lt.Ticket)
                .ThenInclude(t => t.User)
                .OrderByDescending(lt => lt.LikedAt)
                .Select(lt => lt.Ticket)
                .ToListAsync();

            // Build userId → display username map
            var userIds = likedTickets
                .Where(t => t.UserId != null)
                .Select(t => t.UserId!)
                .Distinct()
                .ToList();

            var userDisplayNames = new Dictionary<string, string>();
            foreach (var uid in userIds)
            {
                var u = await _userManager.FindByIdAsync(uid);
                if (u != null)
                {
                    var claims = await _userManager.GetClaimsAsync(u);
                    var displayClaim = claims.FirstOrDefault(c => c.Type == "display_username");
                    if (displayClaim != null && !string.IsNullOrEmpty(displayClaim.Value))
                    {
                        userDisplayNames[uid] = displayClaim.Value;
                    }
                    else
                    {
                        var name = u.UserName ?? string.Empty;
                        var atIdx = name.IndexOf('@');
                        userDisplayNames[uid] = atIdx >= 0 ? name.Substring(0, atIdx) : name;
                    }
                }
            }

            // Build comment counts map
            var ticketIds = likedTickets.Select(t => t.Id).ToList();
            var commentCounts = await _context.TicketComments
                .Where(c => ticketIds.Contains(c.TicketId))
                .GroupBy(c => c.TicketId)
                .Select(g => new { TicketId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TicketId, x => x.Count);

            ViewData["UserDisplayNames"] = userDisplayNames;
            ViewData["CommentCounts"] = commentCounts;
            ViewData["CurrentUserId"] = userId;

            return View(likedTickets);
        }

        public async Task<IActionResult> FriendTickets(string friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(friendId)) return NotFound();

            var friendship = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == currentUserId);
            if (friendship == null) return Forbid(); // Not friends

            var friend = await _userManager.FindByIdAsync(friendId);
            if (friend == null) return NotFound();

            var fClaims = await _userManager.GetClaimsAsync(friend);
            var fName = fClaims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? friend.UserName;
            ViewBag.FriendName = fName;

            var query = _context.Tickets.Where(t => t.UserId == friendId).AsQueryable();

            switch (friendship.SharingSetting)
            {
                case SharingSetting.None:
                    query = query.Where(t => false); // returns empty
                    ViewBag.Message = "This user does not share any tickets with you.";
                    break;
                case SharingSetting.PublicOnly:
                    query = query.Where(t => t.IsPublic);
                    break;
                case SharingSetting.PrivateOnly:
                    query = query.Where(t => !t.IsPublic);
                    break;
                case SharingSetting.All:
                    // no filter
                    break;
            }

            var tickets = await query.OrderBy(t => t.OrderIndex).ToListAsync();

            var likedTicketIds = await _context.LikedTickets
                .Where(lt => lt.UserId == currentUserId)
                .Select(lt => lt.TicketId)
                .ToListAsync();
            ViewData["LikedTicketIds"] = likedTicketIds;

            return View(tickets);
        }

        public async Task<IActionResult> FriendMap(string friendId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(friendId)) return NotFound();

            var friendship = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == currentUserId);
            if (friendship == null) return Forbid(); // Not friends

            var friend = await _userManager.FindByIdAsync(friendId);
            if (friend == null) return NotFound();

            var fClaims = await _userManager.GetClaimsAsync(friend);
            var fName = fClaims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? friend.UserName;
            ViewBag.FriendName = fName;

            var query = _context.Tickets.Where(t => t.UserId == friendId && !string.IsNullOrEmpty(t.Country)).AsQueryable();

            switch (friendship.SharingSetting)
            {
                case SharingSetting.None:
                    query = query.Where(t => false);
                    ViewBag.Message = "This user does not share any tickets with you.";
                    break;
                case SharingSetting.PublicOnly:
                    query = query.Where(t => t.IsPublic);
                    break;
                case SharingSetting.PrivateOnly:
                    query = query.Where(t => !t.IsPublic);
                    break;
                case SharingSetting.All:
                    break;
            }

            var tickets = await query.OrderByDescending(t => t.DepartureTime).ToListAsync();
            return View(tickets);
        }

        public async Task<IActionResult> MapCompletion()
        {
            var userId = _userManager.GetUserId(User);
            var tickets = await _context.Tickets
                .Where(t => t.UserId == userId && !string.IsNullOrEmpty(t.Country))
                .OrderByDescending(t => t.DepartureTime)
                .ToListAsync();

            return View(tickets);
        }


        private bool TicketExists(int id) => _context.Tickets.Any(e => e.Id == id);

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] List<int> orderedIds)
        {
            var tickets = await _context.Tickets.ToListAsync();

            for (int i = 0; i < orderedIds.Count; i++)
            {
                var ticket = tickets.FirstOrDefault(t => t.Id == orderedIds[i]);
                if (ticket != null)
                    ticket.OrderIndex = i;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}
