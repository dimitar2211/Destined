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
    public class FriendsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public FriendsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Friends
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var friendCode = await _context.UserFriendCodes.FirstOrDefaultAsync(fc => fc.UserId == user.Id);
            ViewBag.FriendCode = friendCode?.FriendCode;

            var usernameClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "display_username");
            ViewBag.DisplayUsername = usernameClaim?.Value ?? user.UserName;

            var friends = await _context.Friendships
                .Include(f => f.Friend)
                .Where(f => f.UserId == user.Id)
                .ToListAsync();

            // Fetch display usernames for friends
            var friendDisplayUsernames = new Dictionary<string, string>();
            
            // Get blocked user IDs (both ways)
            var blockedByMe = await _context.BlockedUsers.Where(b => b.BlockerId == user.Id).Select(b => b.BlockedId).ToListAsync();
            var blockedMe = await _context.BlockedUsers.Where(b => b.BlockedId == user.Id).Select(b => b.BlockerId).ToListAsync();
            var allBlockedIds = blockedByMe.Union(blockedMe).ToList();

            friends = friends.Where(f => !allBlockedIds.Contains(f.FriendId)).ToList();

            foreach(var f in friends)
            {
                var fClaims = await _userManager.GetClaimsAsync(f.Friend);
                var fName = fClaims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? f.Friend.UserName;
                friendDisplayUsernames[f.FriendId] = fName;
            }
            ViewBag.FriendDisplayUsernames = friendDisplayUsernames;
            ViewBag.ShowBlockedLink = blockedByMe.Any();

            return View(friends);
        }

        // GET: /Friends/Pending (Sent)
        public async Task<IActionResult> Pending()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var pendingRequests = await _context.FriendRequests
                .Include(fr => fr.Receiver)
                .Where(fr => fr.SenderId == user.Id && fr.Status == FriendRequestStatus.Pending)
                .ToListAsync();

            var receiverDisplayNames = new Dictionary<string, string>();
            foreach(var r in pendingRequests)
            {
                var claims = await _userManager.GetClaimsAsync(r.Receiver);
                receiverDisplayNames[r.ReceiverId] = claims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? r.Receiver.UserName;
            }
            ViewBag.ReceiverDisplayNames = receiverDisplayNames;

            return View(pendingRequests);
        }

        // GET: /Friends/Received
        public async Task<IActionResult> Received()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var receivedRequests = await _context.FriendRequests
                .Include(fr => fr.Sender)
                .Where(fr => fr.ReceiverId == user.Id && fr.Status == FriendRequestStatus.Pending)
                .ToListAsync();

            var senderDisplayNames = new Dictionary<string, string>();
            foreach(var r in receivedRequests)
            {
                var claims = await _userManager.GetClaimsAsync(r.Sender);
                senderDisplayNames[r.SenderId] = claims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? r.Sender.UserName;
            }
            ViewBag.SenderDisplayNames = senderDisplayNames;

            return View(receivedRequests);
        }

        [HttpPost]
        public async Task<IActionResult> Search(string username, string friendCode)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(friendCode))
            {
                TempData["Error"] = "Please provide both username and friend code.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Find users who have the matching Friend Code
            var potentialFriends = await _context.UserFriendCodes
                .Include(fc => fc.User)
                .Where(fc => fc.FriendCode == friendCode.ToUpper().Trim() && fc.UserId != currentUser.Id)
                .ToListAsync();

            IdentityUser foundUser = null;
            // Now filter by display_username claim or actual username
            foreach(var pf in potentialFriends)
            {
                var claims = await _userManager.GetClaimsAsync(pf.User);
                var displayUsername = claims.FirstOrDefault(c => c.Type == "display_username")?.Value;
                if (string.Equals(displayUsername, username, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pf.User.UserName, username, StringComparison.OrdinalIgnoreCase))
                {
                    foundUser = pf.User;
                    break;
                }
            }

            if (foundUser == null)
            {
                TempData["Error"] = "User not found or friend code is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            // Check if already friends
            bool alreadyFriends = await _context.Friendships.AnyAsync(f => f.UserId == currentUser.Id && f.FriendId == foundUser.Id);
            if (alreadyFriends)
            {
                TempData["Error"] = "You are already friends with this user.";
                return RedirectToAction(nameof(Index));
            }

            // Check if blocked (either way)
            bool isBlocked = await _context.BlockedUsers.AnyAsync(b => 
                (b.BlockerId == currentUser.Id && b.BlockedId == foundUser.Id) || 
                (b.BlockerId == foundUser.Id && b.BlockedId == currentUser.Id));
            
            if (isBlocked)
            {
                TempData["Error"] = "User not found or friend code is incorrect."; // Hidden as per requirement
                return RedirectToAction(nameof(Index));
            }

            // Check if request already pending
            bool requestPending = await _context.FriendRequests.AnyAsync(fr => 
                (fr.SenderId == currentUser.Id && fr.ReceiverId == foundUser.Id && fr.Status == FriendRequestStatus.Pending) ||
                (fr.SenderId == foundUser.Id && fr.ReceiverId == currentUser.Id && fr.Status == FriendRequestStatus.Pending));
            
            if (requestPending)
            {
                TempData["Error"] = "There is already a pending friend request between you two.";
                return RedirectToAction(nameof(Index));
            }

            // Create Friend Request
            var fr = new FriendRequest
            {
                SenderId = currentUser.Id,
                ReceiverId = foundUser.Id,
                Status = FriendRequestStatus.Pending
            };
            _context.FriendRequests.Add(fr);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request sent successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == id && r.ReceiverId == user.Id && r.Status == FriendRequestStatus.Pending);

            if (req == null) return NotFound();

            req.Status = FriendRequestStatus.Accepted;

            var friendship1 = new Friendship
            {
                UserId = req.SenderId,
                FriendId = req.ReceiverId
            };
            var friendship2 = new Friendship
            {
                UserId = req.ReceiverId,
                FriendId = req.SenderId
            };

            _context.Friendships.AddRange(friendship1, friendship2);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request accepted!";
            return RedirectToAction(nameof(Received));
        }

        [HttpPost]
        public async Task<IActionResult> Decline(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == id && r.ReceiverId == user.Id && r.Status == FriendRequestStatus.Pending);

            if (req == null) return NotFound();

            req.Status = FriendRequestStatus.Declined;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request declined.";
            return RedirectToAction(nameof(Received));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var req = await _context.FriendRequests.FirstOrDefaultAsync(r => r.Id == id && r.SenderId == user.Id && r.Status == FriendRequestStatus.Pending);

            if (req == null) return NotFound();

            _context.FriendRequests.Remove(req);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Friend request canceled.";
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(string friendId)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var f1 = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == user.Id && f.FriendId == friendId);
            var f2 = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == user.Id);

            if (f1 != null) _context.Friendships.Remove(f1);
            if (f2 != null) _context.Friendships.Remove(f2);

            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Friend removed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSharing(string friendId, SharingSetting setting)
        {
            var user = await _userManager.GetUserAsync(User);
            var friendship = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == user.Id && f.FriendId == friendId);

            if (friendship == null) return NotFound();

            friendship.SharingSetting = setting;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sharing settings updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Block(string friendId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Check if already blocked
            var alreadyBlocked = await _context.BlockedUsers.AnyAsync(b => b.BlockerId == user.Id && b.BlockedId == friendId);
            if (!alreadyBlocked)
            {
                var block = new BlockedUser { BlockerId = user.Id, BlockedId = friendId };
                _context.BlockedUsers.Add(block);

                // IMPORTANT: Remove friendship if it exists
                var f1 = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == user.Id && f.FriendId == friendId);
                var f2 = await _context.Friendships.FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendId == user.Id);
                if (f1 != null) _context.Friendships.Remove(f1);
                if (f2 != null) _context.Friendships.Remove(f2);

                // Also remove pending requests
                var r1 = await _context.FriendRequests.Where(r => (r.SenderId == user.Id && r.ReceiverId == friendId) || (r.SenderId == friendId && r.ReceiverId == user.Id)).ToListAsync();
                _context.FriendRequests.RemoveRange(r1);

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Потребителят е блокиран.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SendRequestById(string friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (currentUser.Id == friendId)
            {
                return BadRequest("You cannot add yourself.");
            }

            // Check if already friends
            bool alreadyFriends = await _context.Friendships.AnyAsync(f => f.UserId == currentUser.Id && f.FriendId == friendId);
            if (alreadyFriends)
            {
                return Json(new { success = false, message = "Вече сте приятели." });
            }

            // Check if blocked
            bool isBlocked = await _context.BlockedUsers.AnyAsync(b => 
                (b.BlockerId == currentUser.Id && b.BlockedId == friendId) || 
                (b.BlockerId == friendId && b.BlockedId == currentUser.Id));
            if (isBlocked)
            {
                return Json(new { success = false, message = "Потребителят не е намерен." });
            }

            // Check pending
            bool requestPending = await _context.FriendRequests.AnyAsync(fr => 
                (fr.SenderId == currentUser.Id && fr.ReceiverId == friendId && fr.Status == FriendRequestStatus.Pending) ||
                (fr.SenderId == friendId && fr.ReceiverId == currentUser.Id && fr.Status == FriendRequestStatus.Pending));
            
            if (requestPending)
            {
                return Json(new { success = false, message = "Вече има изпратена или получена покана." });
            }

            var friendRequest = new FriendRequest
            {
                SenderId = currentUser.Id,
                ReceiverId = friendId,
                Status = FriendRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.FriendRequests.Add(friendRequest);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Поканата за приятелство бе изпратена!" });
        }

        [HttpPost]
        public async Task<IActionResult> Unblock(string friendId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var block = await _context.BlockedUsers.FirstOrDefaultAsync(b => b.BlockerId == user.Id && b.BlockedId == friendId);
            if (block != null)
            {
                _context.BlockedUsers.Remove(block);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Потребителят е отблокиран.";
            return RedirectToAction(nameof(Blocked));
        }

        public async Task<IActionResult> Blocked()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var blockedUsers = await _context.BlockedUsers
                .Include(b => b.Blocked)
                .Where(b => b.BlockerId == user.Id)
                .ToListAsync();

            var blockedDisplayNames = new Dictionary<string, string>();
            var blockedPhotos = new Dictionary<string, string>();
            foreach (var b in blockedUsers)
            {
                var claims = await _userManager.GetClaimsAsync(b.Blocked);
                blockedDisplayNames[b.BlockedId] = claims.FirstOrDefault(c => c.Type == "display_username")?.Value ?? b.Blocked.UserName;
                blockedPhotos[b.BlockedId] = claims.FirstOrDefault(c => c.Type == "profile_picture")?.Value;
            }
            ViewBag.BlockedDisplayNames = blockedDisplayNames;
            ViewBag.BlockedPhotos = blockedPhotos;

            return View(blockedUsers);
        }
    }
}
