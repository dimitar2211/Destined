using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Destined.Models
{
    public class TicketReport
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        public string? ReporterId { get; set; }
        [ForeignKey("ReporterId")]
        public IdentityUser? Reporter { get; set; }

        [Required]
        [StringLength(300, ErrorMessage = "The reason cannot be longer than 300 characters.")]
        public string Reason { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
