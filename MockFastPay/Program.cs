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

app.MapPost("/payments", async (FastPayRequest request) =>
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
    
    var randomId = $"FP-{Random.Shared.Next(100000, 999999)}";
    var status = mode == "rejected" ? "rejected" : "approved";
    var statusDetail = mode == "rejected" 
        ? "Pagamento rejeitado" 
        : "Pagamento aprovado";
    
    var response = new FastPayResponse
    {
        Id = randomId,
        Status = status,
        StatusDetail = statusDetail
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

public class FastPayRequest
{
    public decimal TransactionAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PayerInfo Payer { get; set; } = new();
    public int Installments { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PayerInfo
{
    public string Email { get; set; } = string.Empty;
}

public class FastPayResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusDetail { get; set; } = string.Empty;
}

public class SetModeRequest
{
    public string Mode { get; set; } = string.Empty;
}
