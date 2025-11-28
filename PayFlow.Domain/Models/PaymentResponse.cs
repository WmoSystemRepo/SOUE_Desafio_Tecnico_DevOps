namespace PayFlow.Domain.Models;

public class PaymentResponse
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal Fee { get; set; }
    public decimal NetAmount { get; set; }
}

