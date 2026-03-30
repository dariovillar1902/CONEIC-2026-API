using System;

namespace Coneic.Api.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty; // User Name or Email
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; } = true; // Auto-approve for MVP
    }
}
