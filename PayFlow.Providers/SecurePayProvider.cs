using PayFlow.Domain.Constants;
using PayFlow.Domain.Interfaces;
using PayFlow.Domain.Models;

namespace PayFlow.Providers;

public class SecurePayProvider : IPaymentProvider
{
    private readonly HttpClientWrapper _httpClient;
    private readonly string _baseUrl;
    public string ProviderName => PaymentConstants.ProviderSecurePay;

    public SecurePayProvider(HttpClient httpClient, string baseUrl)
    {
        _httpClient = new HttpClientWrapper(httpClient);
        _baseUrl = baseUrl;
    }

    public decimal CalculateFee(decimal amount)
    {
        return (amount * PaymentConstants.SecurePayFeePercentage) + PaymentConstants.SecurePayFixedFee;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string currency)
    {
        try
        {
            var amountCents = (int)(amount * 100);
            var request = new SecurePayRequest
            {
                AmountCents = amountCents,
                CurrencyCode = currency,
                ClientReference = $"ORD-{DateTime.UtcNow:yyyyMMdd}"
            };

            var response = await _httpClient.PostAsync<SecurePayRequest, SecurePayResponse>(
                $"{_baseUrl}/payments",
                request
            );

            if (response == null)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Empty response from SecurePay",
                    ProviderName = ProviderName
                };
            }

            var status = response.Result.ToLower() == "success" 
                ? PaymentConstants.StatusApproved 
                : PaymentConstants.StatusRejected;

            return new PaymentResult
            {
                ExternalId = response.TransactionId,
                Status = status,
                ProviderName = ProviderName,
                IsSuccess = status == PaymentConstants.StatusApproved
            };
        }
        catch (HttpRequestException ex)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ProviderName = ProviderName
            };
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                IsSuccess = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                ProviderName = ProviderName
            };
        }
    }
}

