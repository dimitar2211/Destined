using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Destined.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "От къде")]
        public string From { get; set; }

        [Required]
        [Display(Name = "До къде")]
        public string To { get; set; }

        [Required]
        [Display(Name = "Час на тръгване")]
        public DateTime DepartureTime { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Брой пътници")]
        public int NumberOfPassengers { get; set; }

        public string? UserId { get; set; }
        public virtual IdentityUser? User { get; set; }

        [Display(Name = "Публичен билет")]
        public bool IsPublic { get; set; } = false;

        // Нови свойства за цветовете на лявата и дясната част
        [Display(Name = "Цвят на лявата част")]
        public string LeftColor { get; set; } = "#e0f2f1";

        [Display(Name = "Цвят на дясната част")]
        public string RightColor { get; set; } = "#ffffff";
        public string TextColor { get; set; } = "#000000"; // ново поле за цвета на текста

    }
}