using System.ComponentModel.DataAnnotations;

namespace Destined.Models
{
    public class UserFriendCode
    {
        [Key]
        public string UserId { get; set; }

        public Microsoft.AspNetCore.Identity.IdentityUser User { get; set; }

        [Required]
        [MaxLength(6)]
        public string FriendCode { get; set; }
    }
}
