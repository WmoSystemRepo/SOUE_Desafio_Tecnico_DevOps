using PayFlow.Domain.Constants;
using PayFlow.Domain.Interfaces;
using PayFlow.Domain.Models;

namespace PayFlow.Providers;

public class FastPayProvider : IPaymentProvider
{
    private readonly HttpClientWrapper _httpClient;
    private readonly string _baseUrl;
    public string ProviderName => PaymentConstants.ProviderFastPay;

    public FastPayProvider(HttpClient httpClient, string baseUrl)
    {
        _httpClient = new HttpClientWrapper(httpClient);
        _baseUrl = baseUrl;
    }

    public decimal CalculateFee(decimal amount)
    {
        return amount * PaymentConstants.FastPayFeePercentage;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string currency)
    {
        try
        {
            var request = new FastPayRequest
            {
                TransactionAmount = amount,
                Currency = currency,
                Payer = new PayerInfo
                {
                    Email = "cliente@teste.com"
                },
                Installments = 1,
                Description = "Compra via FastPay"
            };

            var response = await _httpClient.PostAsync<FastPayRequest, FastPayResponse>(
                $"{_baseUrl}/payments",
                request
            );

            if (response == null)
            {
                return new PaymentResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Empty response from FastPay",
                    ProviderName = ProviderName
                };
            }

            var status = response.Status.ToLower() == PaymentConstants.StatusApproved.ToLower() 
                ? PaymentConstants.StatusApproved 
                : PaymentConstants.StatusRejected;

            return new PaymentResult
            {
                ExternalId = response.Id,
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

