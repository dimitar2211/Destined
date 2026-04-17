using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Destined.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Display(Name = "From")]
        public string? From { get; set; } = string.Empty;

        [Required]
        [Display(Name = "To")]
        public string To { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Departure Time")]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Number of Passengers")]
        public int NumberOfPassengers { get; set; }

        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }

        [Display(Name = "Public Ticket")]
        public bool IsPublic { get; set; } = false;

        [Display(Name = "Comment section")]
        public bool AllowComments { get; set; } = false;

        [Display(Name = "Left part color")]
        public string LeftColor { get; set; } = "#e0f2f1";

        [Display(Name = "Right part color")]
        public string RightColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#000000";
        public int OrderIndex { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Heart color")]
        public string HeartColor { get; set; } = "#ff4757";

        [Display(Name = "Like count color")]
        public string LikeCountColor { get; set; } = "#ff4757";

    }
}