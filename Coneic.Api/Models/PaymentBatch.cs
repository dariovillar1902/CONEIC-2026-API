using System.ComponentModel.DataAnnotations;

namespace Coneic.Api.Models
{
    public class PaymentBatch
    {
        public int Id { get; set; }

        [Required]
        public string DelegationName { get; set; } = string.Empty;

        // URL to the uploaded receipt (transfer proof)
        public string? ReceiptUrl { get; set; }

        // Free text: "Pepito Perez - 1era cuota, Jose Lopez - Pago completo"
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
