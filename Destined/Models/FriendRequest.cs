using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Destined.Models
{
    public class FriendRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public IdentityUser Sender { get; set; }

        [Required]
        public string ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public IdentityUser Receiver { get; set; }

        public FriendRequestStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Declined
    }
}
