using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

var mockMode = builder.Configuration["MOCK_MODE"] ?? "success";
var currentMode = new ConcurrentDictionary<string, string>(new Dictionary<string, string> { ["mode"] = mockMode });

app.MapPost("/payments", async (SecurePayRequest request) =>
{
    var mode = currentMode["mode"];
    
    await Task.Delay(100);
    
    if (mode == "timeout")
    {
        await Task.Delay(60000);
    }
    
    if (mode == "failure")
    {
        return Results.StatusCode(500);
    }
    
    if (mode == "unavailable")
    {
        return Results.StatusCode(503);
    }
    
    var randomId = $"SP-{Random.Shared.Next(10000, 99999)}";
    var result = mode == "rejected" ? "failure" : "success";
    
    var response = new SecurePayResponse
    {
        TransactionId = randomId,
        Result = result
    };
    
    return Results.Ok(response);
})
.WithName("ProcessPayment")
.WithTags("Payments");

app.MapPost("/mock/set-mode", (SetModeRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Mode))
    {
        return Results.BadRequest(new { error = "Mode is required" });
    }
    
    var validModes = new[] { "success", "failure", "rejected", "timeout", "unavailable" };
    if (!validModes.Contains(request.Mode.ToLower()))
    {
        return Results.BadRequest(new { error = $"Invalid mode. Valid modes: {string.Join(", ", validModes)}" });
    }
    
    currentMode["mode"] = request.Mode.ToLower();
    return Results.Ok(new { mode = currentMode["mode"], message = "Mode updated successfully" });
})
.WithName("SetMockMode")
.WithTags("Mock Control");

app.MapGet("/mock/status", () =>
{
    return Results.Ok(new { mode = currentMode["mode"], availableModes = new[] { "success", "failure", "rejected", "timeout", "unavailable" } });
})
.WithName("GetMockStatus")
.WithTags("Mock Control");

app.Run();

public class SecurePayRequest
{
    public int AmountCents { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string ClientReference { get; set; } = string.Empty;
}

public class SecurePayResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
}

public class SetModeRequest
{
    public string Mode { get; set; } = string.Empty;
}
