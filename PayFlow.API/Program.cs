using PayFlow.Domain.Constants;
using PayFlow.Domain.Models;
using PayFlow.Providers;
using PayFlow.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

var fastPayBaseUrl = configuration["PaymentProviders:FastPay:BaseUrl"] ?? "https://api.fastpay.com";
var securePayBaseUrl = configuration["PaymentProviders:SecurePay:BaseUrl"] ?? "https://api.securepay.com";

builder.Services.AddHttpClient("FastPay", client =>
{
    client.BaseAddress = new Uri(fastPayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("SecurePay", client =>
{
    client.BaseAddress = new Uri(securePayBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<FastPayProvider>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("FastPay");
    return new FastPayProvider(httpClient, fastPayBaseUrl);
});

builder.Services.AddScoped<SecurePayProvider>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("SecurePay");
    return new SecurePayProvider(httpClient, securePayBaseUrl);
});

builder.Services.AddScoped<PaymentService>(serviceProvider =>
{
    var fastPayProvider = serviceProvider.GetRequiredService<FastPayProvider>();
    var securePayProvider = serviceProvider.GetRequiredService<SecurePayProvider>();
    return new PaymentService(fastPayProvider, securePayProvider);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapPost("/payments", async (
    [FromServices] PaymentService paymentService,
    [FromBody] PaymentRequest request) =>
{
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(request);
    
    if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
    {
        var errors = validationResults.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrEmpty(e)).ToList();
        return Results.BadRequest(new { errors });
    }

    if (request.Currency != PaymentConstants.CurrencyBRL)
    {
        return Results.BadRequest(new { error = $"Currency must be {PaymentConstants.CurrencyBRL}" });
    }

    if (request.Amount < PaymentConstants.MinPaymentAmount || request.Amount > PaymentConstants.MaxPaymentAmount)
    {
        return Results.BadRequest(new { error = $"Amount must be between {PaymentConstants.MinPaymentAmount} and {PaymentConstants.MaxPaymentAmount}" });
    }

    try
    {
        var response = await paymentService.ProcessPaymentAsync(request);
        return Results.Ok(response);
    }
    catch (Exception)
    {
        return Results.Problem(
            detail: "Internal server error",
            statusCode: 500);
    }
}).WithName("ProcessPayment").WithTags("Payments");

app.Run();
