using PayFlow.Domain.Constants;
using PayFlow.Domain.Interfaces;
using PayFlow.Domain.Models;

namespace PayFlow.Services;

public class PaymentService
{
    private readonly IPaymentProvider _fastPayProvider;
    private readonly IPaymentProvider _securePayProvider;
    private static int _paymentIdCounter = 1;
    private static readonly object _lockObject = new();

    public PaymentService(IPaymentProvider fastPayProvider, IPaymentProvider securePayProvider)
    {
        _fastPayProvider = fastPayProvider;
        _securePayProvider = securePayProvider;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
    {
        var primaryProvider = request.Amount < PaymentConstants.ThresholdAmount ? _fastPayProvider : _securePayProvider;
        var fallbackProvider = request.Amount < PaymentConstants.ThresholdAmount ? _securePayProvider : _fastPayProvider;

        var result = await primaryProvider.ProcessPaymentAsync(request.Amount, request.Currency);

        if (!result.IsSuccess)
        {
            result = await fallbackProvider.ProcessPaymentAsync(request.Amount, request.Currency);
        }

        IPaymentProvider providerUsed;
        if (result.IsSuccess)
        {
            providerUsed = result.ProviderName == PaymentConstants.ProviderFastPay ? _fastPayProvider : _securePayProvider;
        }
        else
        {
            providerUsed = primaryProvider;
            result.Status = PaymentConstants.StatusRejected;
            result.ProviderName = primaryProvider.ProviderName;
        }

        var fee = providerUsed.CalculateFee(request.Amount);
        var netAmount = request.Amount - fee;

        int paymentId;
        lock (_lockObject)
        {
            paymentId = _paymentIdCounter++;
        }

        return new PaymentResponse
        {
            Id = paymentId,
            ExternalId = result.ExternalId ?? string.Empty,
            Status = result.Status,
            Provider = result.ProviderName,
            GrossAmount = request.Amount,
            Fee = fee,
            NetAmount = netAmount
        };
    }
}

