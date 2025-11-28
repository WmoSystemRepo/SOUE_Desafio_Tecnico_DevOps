namespace PayFlow.Domain.Models;

public class SecurePayRequest
{
    public int AmountCents { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string ClientReference { get; set; } = string.Empty;
}

