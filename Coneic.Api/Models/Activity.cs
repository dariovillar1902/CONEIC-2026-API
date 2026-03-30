using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        public int? SpeakerId { get; set; }
        public Speaker? Speaker { get; set; }
    }
}
