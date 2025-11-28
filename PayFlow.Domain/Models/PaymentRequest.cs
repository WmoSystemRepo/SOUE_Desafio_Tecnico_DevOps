using System.ComponentModel.DataAnnotations;

namespace PayFlow.Domain.Models;

public class PaymentRequest
{
    [Range(0.01, 1000000.00, ErrorMessage = "Amount must be between 0.01 and 1,000,000.00")]
    public decimal Amount { get; set; }
    
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be exactly 3 characters")]
    public string Currency { get; set; } = string.Empty;
}

