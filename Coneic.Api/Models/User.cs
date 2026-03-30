using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // In a real app, hash this!

        [Required]
        public string Role { get; set; } = "visitor"; // admin, delegate, assistant, visitor

        public string? DelegationName { get; set; } // Optional: For delegates
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
