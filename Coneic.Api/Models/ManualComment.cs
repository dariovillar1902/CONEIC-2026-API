using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models;

public class ManualComment
{
    public int Id { get; set; }

    [Required]
    public string AuthorEmail { get; set; } = string.Empty;

    [Required]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    public string SectionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
