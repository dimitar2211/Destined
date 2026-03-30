namespace Destined.Models
{
    public class LikedTicket
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public virtual Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }
        public int TicketId { get; set; }
        public virtual Ticket? Ticket { get; set; }
        public DateTime LikedAt { get; set; } = DateTime.UtcNow;
    }
}
