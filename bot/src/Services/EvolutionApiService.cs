using System.Text;
using System.Text.Json;

namespace MaceioBot.Services;

public class EvolutionApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EvolutionApiService> _logger;
    private readonly string _instanceName;

    public EvolutionApiService(HttpClient httpClient, IConfiguration config, ILogger<EvolutionApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _instanceName = config["EvolutionApi:InstanceName"] ?? "maceio-whatsapp";
        
        var baseUrl = config["EvolutionApi:BaseUrl"] ?? "http://localhost:8080";
        var apiKey = config["EvolutionApi:ApiKey"] ?? "";
        
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("apikey", apiKey);
    }

    public async Task SendTextMessageAsync(string phone, string text)
    {
        var payload = new
        {
            number = phone,
            text = text
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"/message/sendText/{_instanceName}", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao enviar mensagem: {Error}", error);
        }
    }

    public async Task SendButtonMessageAsync(string phone, string text, List<string> buttons)
    {
        // Evolution API v2 usa sendList ou sendButtons dependendo da versão
        // Usando sendButtons com formato compatível
        var buttonList = buttons.Select((b, i) => new
        {
            buttonId = $"btn_{i}",
            buttonText = new { displayText = b }
        }).ToList();

        var payload = new
        {
            number = phone,
            title = "Maceió AutoPosto",
            description = text,
            footer = "Responda clicando em um botão",
            buttons = buttonList
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync($"/message/sendButtons/{_instanceName}", content);
        
        if (!response.IsSuccessStatusCode)
        {
            // Fallback para texto simples se botões não funcionarem
            var fallbackText = $"{text}\n\n" + string.Join("\n", buttons.Select((b, i) => $"{i + 1}. {b}"));
            await SendTextMessageAsync(phone, fallbackText);
        }
    }

    public async Task<bool> CreateInstanceAsync()
    {
        var payload = new
        {
            instanceName = _instanceName,
            integration = "WHATSAPP-BAILEYS",
            qrcode = true
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("/instance/create", content);
        
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> GetQrCodeAsync()
    {
        var response = await _httpClient.GetAsync($"/instance/connect/{_instanceName}");
        
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("base64", out var base64))
            {
                return base64.GetString();
            }
        }
        
        return null;
    }

    public async Task SetWebhookAsync(string webhookUrl)
    {
        var payload = new
        {
            webhook = new
            {
                enabled = true,
                url = webhookUrl,
                webhookByEvents = false,
                events = new[] { "MESSAGES_UPSERT" }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await _httpClient.PostAsync($"/webhook/set/{_instanceName}", content);
    }
}
