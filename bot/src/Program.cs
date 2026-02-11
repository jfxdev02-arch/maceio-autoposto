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
// AntibanService como Singleton e HostedService para processar a fila global
builder.Services.AddSingleton<AntibanService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AntibanService>());

builder.Services.AddScoped<QuestionnaireFlow>();

var app = builder.Build();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapPost("/webhook/evolution", async (HttpContext context, IServiceProvider sp) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("event", out var eventProp))
        {
            if (eventProp.GetString() != "messages.upsert") return Results.Ok();
        }

        var (phone, pushName, text) = WebhookParser.Parse(root);
        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(text)) return Results.Ok();

        logger.LogInformation("Mensagem recebida de {Phone}: {Text}", phone, text);

        using var scope = sp.CreateScope();
        var flow = scope.ServiceProvider.GetRequiredService<QuestionnaireFlow>();
        await flow.ProcessMessageAsync(phone, pushName ?? "Cliente", text);
    }
    catch (Exception ex) { logger.LogError(ex, "Erro no webhook"); }
    return Results.Ok();
});

app.MapGet("/api/antiban/stats", (AntibanService antiban) => Results.Ok(antiban.GetStats()));

app.Run();
