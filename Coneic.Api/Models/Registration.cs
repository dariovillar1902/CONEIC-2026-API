using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    public class Registration
    {
        public int Id { get; set; }
        
        // Personal Data
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string Lastname { get; set; } = string.Empty;
        
        [Required]
        public string Dni { get; set; } = string.Empty;
        
        [Required]
        public string Phone { get; set; } = string.Empty;

        // Academic Data
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Faculty { get; set; } = string.Empty;
        
        // Stored as a simple filename or URL for now
        public string? CertificateFileName { get; set; }

        // Health & Emergency
        public string? BloodType { get; set; }
        public string? MedicalConditions { get; set; }
        
        [Required]
        public string EmergencyContactName { get; set; } = string.Empty;
        
        [Required]
        public string EmergencyContactPhone { get; set; } = string.Empty;

        // Status Management
        public string Status { get; set; } = "Pending"; // Pending, Validated, Paid
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string? PaymentReceiptUrl { get; set; }
        
        public string StageName { get; set; } = "Unknown";
        public decimal Price { get; set; }

        // New Fields (Phase 3)
        public bool ParticipatedInJoreic { get; set; }
        public bool InterestedInMaccaferri { get; set; }
        public string? DietaryRestrictions { get; set; }
        public string? PaymentMethod { get; set; } // MercadoPago, Transferencia, Efectivo
        public decimal AmountPaid { get; set; }
        public decimal AmountPending { get; set; }

        // Delegate management fields
        public bool IsEnabled { get; set; } = false;
        public string? PaymentCondition { get; set; } // null, "PagoCompleto", "PrimeraCuota", "SegundaCuota"

        // Free-text notes visible to delegates (replaces the old per-registration comprobante link)
        public string? Observations { get; set; }

        // International students (registration-international page)
        public bool IsInternational { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? University { get; set; }
        public string? AttendanceConfidence { get; set; }
    }
    
    public enum RegistrationStatus 
    {
        Pending,
        Validated,
        Paid
    }
}
