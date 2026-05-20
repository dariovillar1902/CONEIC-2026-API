using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    /// <summary>
    /// A group payment batch uploaded by a delegate.
    /// One batch = one transfer receipt + per-person amount assignments.
    /// </summary>
    public class PaymentBatch
    {
        public int Id { get; set; }

        /// <summary>Email of the delegate who created the batch.</summary>
        [Required]
        public string DelegateEmail { get; set; } = string.Empty;

        /// <summary>Legacy field — kept for backward compat with existing data.</summary>
        public string? DelegationName { get; set; }

        /// <summary>URL to the uploaded receipt (transfer proof image or PDF).</summary>
        public string? ReceiptUrl { get; set; }

        /// <summary>Free-text note about this batch (optional).</summary>
        public string? Description { get; set; }

        /// <summary>Per-person amount assignments included in this batch.</summary>
        public List<BatchAssignment> Assignments { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Whether the delegate requested an invoice for this batch.</summary>
        public bool NeedsInvoice { get; set; } = false;
    }

    /// <summary>Links one registration to a specific payment type within a batch.</summary>
    public class BatchAssignment
    {
        public int RegistrationId { get; set; }
        public string PersonName { get; set; } = string.Empty;

        /// <summary>Amount paid by this person in this batch (e.g. 55000 or 100000).</summary>
        public decimal Amount { get; set; }

        /// <summary>"Pagó Completo" | "Pagó 1° Cuota" | "No Pagó"</summary>
        public string? PaymentType { get; set; }
    }
}
