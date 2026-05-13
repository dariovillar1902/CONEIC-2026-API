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

        /// <summary>Legacy single-faculty identifier — kept for backward compat.</summary>
        public string? DelegationName { get; set; }

        /// <summary>ANEIC region ID (e.g. "Este", "Oeste"). Set for delegates.</summary>
        public string? Filial { get; set; }

        /// <summary>All faculties managed by this delegate. Empty for non-delegates.</summary>
        public List<string> ManagedFaculties { get; set; } = new();

        /// <summary>True when the account was auto-created and the user must set a new password.</summary>
        public bool MustChangePassword { get; set; } = false;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }
}
