namespace PayFlow.Domain.Models;

public class SecurePayResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}

