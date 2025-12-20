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
        public async Task<IActionResult> Edit(int id, [Bind("Id,From,To,DepartureTime,NumberOfPassengers,IsPublic,LeftColor,RightColor,TextColor,Country")] Ticket ticket)
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
        public async Task<IActionResult> PublicTickets()
        {
            var publicTickets = await _context.Tickets
                .Where(t => t.IsPublic)
                .Include(t => t.User)
                .OrderBy(t => Guid.NewGuid())
                .ToListAsync();

            return View(publicTickets);
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
