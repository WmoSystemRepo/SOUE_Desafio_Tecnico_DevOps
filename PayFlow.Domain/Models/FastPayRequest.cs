namespace PayFlow.Domain.Models;

public class FastPayRequest
{
    public decimal TransactionAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PayerInfo Payer { get; set; } = new();
    public int Installments { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PayerInfo
{
    public string Email { get; set; } = string.Empty;
}

