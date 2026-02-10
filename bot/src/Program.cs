using System.Text.Json;
using MaceioBot.Data;
using MaceioBot.Flow;
using MaceioBot.Services;
using MaceioBot.Webhooks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddHttpClient<EvolutionApiService>();
builder.Services.AddScoped<QuestionnaireFlow>();

var app = builder.Build();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Webhook endpoint para Evolution API
app.MapPost("/webhook/evolution", async (HttpContext context, IServiceProvider sp) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Webhook received: {Body}", body);

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // Verificar se é evento de mensagem
        if (root.TryGetProperty("event", out var eventProp))
        {
            var eventType = eventProp.GetString();
            if (eventType != "messages.upsert")
            {
                return Results.Ok();
            }
        }

        var (phone, pushName, text) = WebhookParser.Parse(root);

        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(text))
        {
            return Results.Ok();
        }

        logger.LogInformation("Processing message from {Phone}: {Text}", phone, text);

        using var scope = sp.CreateScope();
        var flow = scope.ServiceProvider.GetRequiredService<QuestionnaireFlow>();
        await flow.ProcessMessageAsync(phone, pushName ?? "Cliente", text);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing webhook");
    }

    return Results.Ok();
});

// Endpoints de administração do bot
app.MapGet("/api/instance/create", async (EvolutionApiService evolution) =>
{
    var success = await evolution.CreateInstanceAsync();
    return success ? Results.Ok("Instance created") : Results.BadRequest("Failed to create instance");
});

app.MapGet("/api/instance/qrcode", async (EvolutionApiService evolution) =>
{
    var qrCode = await evolution.GetQrCodeAsync();
    return qrCode != null ? Results.Ok(new { qrcode = qrCode }) : Results.NotFound("QR Code not available");
});

app.MapPost("/api/webhook/configure", async (EvolutionApiService evolution, IConfiguration config) =>
{
    var botUrl = config["Bot:PublicUrl"] ?? "http://bot:5000";
    await evolution.SetWebhookAsync($"{botUrl}/webhook/evolution");
    return Results.Ok("Webhook configured");
});

app.Run();
