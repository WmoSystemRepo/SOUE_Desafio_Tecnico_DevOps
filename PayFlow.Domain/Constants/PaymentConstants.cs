namespace PayFlow.Domain.Constants;

public static class PaymentConstants
{
    public const string ProviderFastPay = "FastPay";
    public const string ProviderSecurePay = "SecurePay";
    
    public const string StatusApproved = "approved";
    public const string StatusRejected = "rejected";
    
    public const decimal ThresholdAmount = 100.00m;
    
    public const decimal FastPayFeePercentage = 0.0349m;
    
    public const decimal SecurePayFeePercentage = 0.0299m;
    public const decimal SecurePayFixedFee = 0.40m;
    
    public const string CurrencyBRL = "BRL";
    
    public const decimal MaxPaymentAmount = 1000000.00m;
    public const decimal MinPaymentAmount = 0.01m;
}

