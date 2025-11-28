using Moq;
using PayFlow.Domain.Constants;
using PayFlow.Domain.Interfaces;
using PayFlow.Domain.Models;
using PayFlow.Services;
using Xunit;

namespace PayFlow.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPayment_AmountLessThanThreshold_ShouldUseFastPay()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "FP-123",
                ProviderName = PaymentConstants.ProviderFastPay
            });
        fastPayMock.Setup(p => p.CalculateFee(50.00m)).Returns(1.745m);

        var securePayMock = new Mock<IPaymentProvider>();
        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 50.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderFastPay, result.Provider);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        Assert.Equal(1.745m, result.Fee);
        Assert.Equal(48.255m, result.NetAmount);
        Assert.Equal(50.00m, result.GrossAmount);
        fastPayMock.Verify(p => p.ProcessPaymentAsync(50.00m, "BRL"), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_AmountGreaterThanOrEqualThreshold_ShouldUseSecurePay()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "SP-456",
                ProviderName = PaymentConstants.ProviderSecurePay
            });
        securePayMock.Setup(p => p.CalculateFee(150.00m)).Returns(4.885m);

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 150.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderSecurePay, result.Provider);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        Assert.Equal(4.885m, result.Fee);
        Assert.Equal(145.115m, result.NetAmount);
        securePayMock.Verify(p => p.ProcessPaymentAsync(150.00m, "BRL"), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_AmountExactlyThreshold_ShouldUseSecurePay()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "SP-789",
                ProviderName = PaymentConstants.ProviderSecurePay
            });
        securePayMock.Setup(p => p.CalculateFee(PaymentConstants.ThresholdAmount)).Returns(3.39m);

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = PaymentConstants.ThresholdAmount, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderSecurePay, result.Provider);
        securePayMock.Verify(p => p.ProcessPaymentAsync(PaymentConstants.ThresholdAmount, "BRL"), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_FastPayFails_ShouldUseSecurePayAsFallback()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentConstants.StatusRejected,
                ProviderName = PaymentConstants.ProviderFastPay,
                ErrorMessage = "Provider unavailable"
            });

        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "SP-FALLBACK",
                ProviderName = PaymentConstants.ProviderSecurePay
            });
        securePayMock.Setup(p => p.CalculateFee(50.00m)).Returns(1.895m);

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 50.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderSecurePay, result.Provider);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        fastPayMock.Verify(p => p.ProcessPaymentAsync(50.00m, "BRL"), Times.Once);
        securePayMock.Verify(p => p.ProcessPaymentAsync(50.00m, "BRL"), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_SecurePayFails_ShouldUseFastPayAsFallback()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "FP-FALLBACK",
                ProviderName = PaymentConstants.ProviderFastPay
            });
        fastPayMock.Setup(p => p.CalculateFee(150.00m)).Returns(5.235m);

        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentConstants.StatusRejected,
                ProviderName = PaymentConstants.ProviderSecurePay,
                ErrorMessage = "Provider unavailable"
            });

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 150.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderFastPay, result.Provider);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        securePayMock.Verify(p => p.ProcessPaymentAsync(150.00m, "BRL"), Times.Once);
        fastPayMock.Verify(p => p.ProcessPaymentAsync(150.00m, "BRL"), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_BothProvidersFail_ShouldReturnRejected()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentConstants.StatusRejected,
                ProviderName = PaymentConstants.ProviderFastPay,
                ErrorMessage = "Provider unavailable"
            });
        fastPayMock.Setup(p => p.CalculateFee(50.00m)).Returns(1.745m);

        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = false,
                Status = PaymentConstants.StatusRejected,
                ProviderName = PaymentConstants.ProviderSecurePay,
                ErrorMessage = "Provider unavailable"
            });

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 50.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderFastPay, result.Provider);
        Assert.Equal(PaymentConstants.StatusRejected, result.Status);
        Assert.Equal(1.745m, result.Fee);
        Assert.Equal(48.255m, result.NetAmount);
    }

    [Fact]
    public async Task ProcessPayment_ShouldGenerateSequentialIds()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "FP-1",
                ProviderName = PaymentConstants.ProviderFastPay
            });
        fastPayMock.Setup(p => p.CalculateFee(It.IsAny<decimal>())).Returns(1.0m);

        var securePayMock = new Mock<IPaymentProvider>();
        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request1 = new PaymentRequest { Amount = 50.00m, Currency = "BRL" };
        var result1 = await service.ProcessPaymentAsync(request1);

        var request2 = new PaymentRequest { Amount = 60.00m, Currency = "BRL" };
        var result2 = await service.ProcessPaymentAsync(request2);

        Assert.True(result2.Id > result1.Id);
    }

    [Fact]
    public async Task ProcessPayment_ShouldCalculateNetAmountCorrectly()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        var securePayMock = new Mock<IPaymentProvider>();
        securePayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderSecurePay);
        securePayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult
            {
                IsSuccess = true,
                Status = PaymentConstants.StatusApproved,
                ExternalId = "SP-CALC",
                ProviderName = PaymentConstants.ProviderSecurePay
            });
        securePayMock.Setup(p => p.CalculateFee(100.00m)).Returns(3.39m);

        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 100.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(100.00m, result.GrossAmount);
        Assert.Equal(3.39m, result.Fee);
        Assert.Equal(96.61m, result.NetAmount);
    }
}

