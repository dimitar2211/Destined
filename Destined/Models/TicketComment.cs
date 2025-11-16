using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Destined.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }

        public Ticket Ticket { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public int? ParentCommentId { get; set; }
        public TicketComment? ParentComment { get; set; }

        public ICollection<TicketComment> Replies { get; set; }
    }
}
