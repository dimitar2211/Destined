using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Destined.Models
{
    public class BlockedUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string BlockerId { get; set; }

        [ForeignKey("BlockerId")]
        public IdentityUser Blocker { get; set; }

        [Required]
        public string BlockedId { get; set; }

        [ForeignKey("BlockedId")]
        public IdentityUser Blocked { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
