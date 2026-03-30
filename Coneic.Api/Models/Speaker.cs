using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    public class Speaker
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        [MaxLength(300)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string LinkedInUrl { get; set; } = string.Empty;
    }
}
