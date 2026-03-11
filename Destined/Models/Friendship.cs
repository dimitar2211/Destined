using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Destined.Models
{
    public class Friendship
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public IdentityUser User { get; set; }

        [Required]
        public string FriendId { get; set; }

        [ForeignKey("FriendId")]
        public IdentityUser Friend { get; set; }

        public SharingSetting SharingSetting { get; set; } = SharingSetting.All;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum SharingSetting
    {
        All,
        PublicOnly,
        PrivateOnly,
        None
    }
}
