using PayFlow.Domain.Models;

namespace PayFlow.Domain.Interfaces;

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, string currency);
    decimal CalculateFee(decimal amount);
}

