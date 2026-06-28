using ComplianceGuardian.Api;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddSingleton<ComplianceService>();

builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Compliance Guardian API",
        Version     = "v1",
        Description = "AI-powered contract and compliance risk analysis. " +
                      "POST a contract or question to /analyze and receive a structured risk report.",
    });
});

// ── App ───────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compliance Guardian API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Compliance Guardian API";
});

// ── Endpoints ─────────────────────────────────────────────────────────────────

// GET / — redirect to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription();

// GET /health — liveness probe
app.MapGet("/health", () =>
    Results.Ok(new HealthResponse("Healthy", "1.0.0", DateTimeOffset.UtcNow)))
   .WithName("Health")
   .WithSummary("Liveness check")
   .WithDescription("Returns the API health status.");

// POST /analyze — main endpoint
app.MapPost("/analyze", async (AnalyzeRequest request, ComplianceService service) =>
{
    if (string.IsNullOrWhiteSpace(request.DocumentText) && string.IsNullOrWhiteSpace(request.Question))
        return Results.BadRequest(new { error = "Provide at least one of: documentText, question." });

    var result = await service.AnalyzeAsync(request);
    return Results.Ok(result);
})
.WithName("Analyze")
.WithSummary("Analyze a contract or compliance question")
.WithDescription("""
    Submit contract text or a compliance question. Returns:
    - Overall risk level (Low / Medium / High / Critical)
    - Risk findings per clause type
    - Compliance gaps against specified policies (GDPR, SOC 2, HIPAA, ISO 27001)
    - Executive summary, recommendations, and action items
    """);

app.Run();
