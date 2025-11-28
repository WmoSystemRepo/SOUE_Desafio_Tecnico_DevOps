using System.Net;
using System.Net.Http.Json;
using Moq;
using Moq.Protected;
using PayFlow.Domain.Constants;
using PayFlow.Domain.Models;
using PayFlow.Providers;
using Xunit;

namespace PayFlow.Tests.Providers;

public class SecurePayProviderTests
{
    [Fact]
    public void CalculateFee_ShouldReturnCorrectPercentagePlusFixed()
    {
        var httpClient = new HttpClient();
        var provider = new SecurePayProvider(httpClient, "http://test.com");

        var amount = 100.00m;
        var expectedFee = (amount * PaymentConstants.SecurePayFeePercentage) + PaymentConstants.SecurePayFixedFee;
        var actualFee = provider.CalculateFee(amount);

        Assert.Equal(expectedFee, actualFee);
        Assert.Equal(3.39m, actualFee);
    }

    [Fact]
    public void ProviderName_ShouldReturnSecurePay()
    {
        var httpClient = new HttpClient();
        var provider = new SecurePayProvider(httpClient, "http://test.com");

        Assert.Equal(PaymentConstants.ProviderSecurePay, provider.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SuccessfulResponse_ShouldReturnApproved()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SecurePayResponse
            {
                TransactionId = "SP-123",
                Result = "success"
            })
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        Assert.Equal("SP-123", result.ExternalId);
        Assert.Equal(PaymentConstants.ProviderSecurePay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NonSuccessResponse_ShouldReturnRejected()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SecurePayResponse
            {
                TransactionId = "SP-456",
                Result = "failed"
            })
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentConstants.StatusRejected, result.Status);
        Assert.Equal(PaymentConstants.ProviderSecurePay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_NullResponse_ShouldReturnFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderSecurePay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_HttpRequestException_ShouldReturnFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Network error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderSecurePay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_GenericException_ShouldReturnFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderSecurePay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldConvertAmountToCents()
    {
        var capturedRequest = new HttpRequestMessage();
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SecurePayResponse
            {
                TransactionId = "SP-CENTS",
                Result = "success"
            })
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        await provider.ProcessPaymentAsync(123.45m, "BRL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldFormatRequestCorrectly()
    {
        var capturedRequest = new HttpRequestMessage();
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SecurePayResponse
            {
                TransactionId = "SP-FORMAT",
                Result = "success"
            })
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Contains("/payments", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldGenerateClientReference()
    {
        var capturedRequest = new HttpRequestMessage();
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new SecurePayResponse
            {
                TransactionId = "SP-REF",
                Result = "success"
            })
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };

        var provider = new SecurePayProvider(httpClient, "http://test.com");
        await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.NotNull(capturedRequest);
    }

    [Fact]
    public void CalculateFee_WithZeroAmount_ShouldReturnFixedFee()
    {
        var httpClient = new HttpClient();
        var provider = new SecurePayProvider(httpClient, "http://test.com");

        var fee = provider.CalculateFee(0.00m);

        Assert.Equal(PaymentConstants.SecurePayFixedFee, fee);
    }

    [Fact]
    public void CalculateFee_WithLargeAmount_ShouldCalculateCorrectly()
    {
        var httpClient = new HttpClient();
        var provider = new SecurePayProvider(httpClient, "http://test.com");

        var amount = 1000.00m;
        var expectedFee = (amount * PaymentConstants.SecurePayFeePercentage) + PaymentConstants.SecurePayFixedFee;
        var actualFee = provider.CalculateFee(amount);

        Assert.Equal(expectedFee, actualFee);
        Assert.Equal(30.30m, actualFee);
    }
}

