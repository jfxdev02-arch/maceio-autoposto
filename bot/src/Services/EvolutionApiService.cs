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
        _instanceName = config["EvolutionApi:InstanceName"] ?? "MaceioAutoPosto";
        
        var baseUrl = config["EvolutionApi:BaseUrl"] ?? "http://evolution:8080";
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
            _logger.LogError("Erro ao enviar mensagem via Evolution: {Error}", error);
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
