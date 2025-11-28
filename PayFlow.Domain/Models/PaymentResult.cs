namespace PayFlow.Domain.Models;

public class PaymentResult
{
    public string ExternalId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

