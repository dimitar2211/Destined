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
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ChatController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(string friendId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Check if blocked
            var isBlocked = await _context.BlockedUsers.AnyAsync(b => 
                (b.BlockerId == user.Id && b.BlockedId == friendId) || 
                (b.BlockerId == friendId && b.BlockedId == user.Id));
            if (isBlocked) return Forbid();

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == user.Id))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    senderId = m.SenderId,
                    isMine = m.SenderId == user.Id,
                    isLiked = m.IsLiked,
                    createdAt = m.CreatedAt.ToString("HH:mm"),
                    modifiedAt = m.ModifiedAt
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> Send(string friendId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Check if blocked
            var isBlocked = await _context.BlockedUsers.AnyAsync(b => 
                (b.BlockerId == user.Id && b.BlockedId == friendId) || 
                (b.BlockerId == friendId && b.BlockedId == user.Id));
            if (isBlocked) return Forbid();

            // Verify they are friends
            var isFriend = await _context.Friendships.AnyAsync(f => f.UserId == user.Id && f.FriendId == friendId);
            if (!isFriend) return Forbid();

            var message = new ChatMessage
            {
                SenderId = user.Id,
                ReceiverId = friendId,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = message.Id });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int messageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return NotFound();

            // Only the receiver can like the message? 
            // when a double click is made on someone else's message to be liked
            
            if (message.ReceiverId != user.Id) return Forbid();

            message.IsLiked = !message.IsLiked;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isLiked = message.IsLiked });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int messageId, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return NotFound();

            // Only the sender can edit their own message
            if (message.SenderId != user.Id) return Forbid();

            message.Content = content;
            message.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int messageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null) return NotFound();

            // Only the sender can delete their own message
            if (message.SenderId != user.Id) return Forbid();

            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
