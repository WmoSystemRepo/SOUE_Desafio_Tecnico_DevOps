using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Moq.Protected;
using PayFlow.Domain.Constants;
using PayFlow.Domain.Models;
using PayFlow.Providers;
using Xunit;

namespace PayFlow.Tests.Providers;

public class FastPayProviderTests
{
    [Fact]
    public void CalculateFee_ShouldReturnCorrectPercentage()
    {
        var httpClient = new HttpClient();
        var provider = new FastPayProvider(httpClient, "http://test.com");

        var amount = 100.00m;
        var expectedFee = amount * PaymentConstants.FastPayFeePercentage;
        var actualFee = provider.CalculateFee(amount);

        Assert.Equal(expectedFee, actualFee);
        Assert.Equal(3.49m, actualFee);
    }

    [Fact]
    public void ProviderName_ShouldReturnFastPay()
    {
        var httpClient = new HttpClient();
        var provider = new FastPayProvider(httpClient, "http://test.com");

        Assert.Equal(PaymentConstants.ProviderFastPay, provider.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SuccessfulResponse_ShouldReturnApproved()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new FastPayResponse
            {
                Id = "FP-123",
                Status = PaymentConstants.StatusApproved,
                StatusDetail = "Approved"
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        Assert.Equal("FP-123", result.ExternalId);
        Assert.Equal(PaymentConstants.ProviderFastPay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_RejectedResponse_ShouldReturnRejected()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new FastPayResponse
            {
                Id = "FP-456",
                Status = PaymentConstants.StatusRejected,
                StatusDetail = "Rejected"
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentConstants.StatusRejected, result.Status);
        Assert.Equal(PaymentConstants.ProviderFastPay, result.ProviderName);
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderFastPay, result.ProviderName);
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Network error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderFastPay, result.ProviderName);
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.False(result.IsSuccess);
        Assert.Contains("Unexpected error", result.ErrorMessage);
        Assert.Equal(PaymentConstants.ProviderFastPay, result.ProviderName);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldFormatRequestCorrectly()
    {
        var capturedRequest = new HttpRequestMessage();
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new FastPayResponse
            {
                Id = "FP-789",
                Status = PaymentConstants.StatusApproved,
                StatusDetail = "Approved"
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        await provider.ProcessPaymentAsync(75.50m, "BRL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Contains("/payments", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task ProcessPaymentAsync_StatusCaseInsensitive_ShouldHandleCorrectly()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new FastPayResponse
            {
                Id = "FP-CASE",
                Status = "APPROVED",
                StatusDetail = "Approved"
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

        var provider = new FastPayProvider(httpClient, "http://test.com");
        var result = await provider.ProcessPaymentAsync(100.00m, "BRL");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
    }
}

