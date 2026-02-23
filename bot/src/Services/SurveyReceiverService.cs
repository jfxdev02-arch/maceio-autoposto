using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MaceioBot.Data;

namespace MaceioBot.Services;

public class SurveyReceiverService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SurveyReceiverService> _logger;

    public SurveyReceiverService(AppDbContext db, ILogger<SurveyReceiverService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ProcessIncomingMessageAsync(string phone, string? pushName, string messageText)
    {
        var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
        
        _logger.LogInformation("Processando mensagem de {Phone}: {Message}", cleanPhone, messageText);

        // Tenta extrair o codigo do sorteio da mensagem
        var luckyNumber = ExtractLuckyNumber(messageText);
        
        if (string.IsNullOrEmpty(luckyNumber))
        {
            _logger.LogWarning("Nao foi possivel extrair o codigo do sorteio da mensagem");
            return;
        }

        // Busca o respondente pelo codigo do sorteio
        var respondent = await _db.Respondents.FirstOrDefaultAsync(r => r.LuckyNumber == luckyNumber);
        
        if (respondent == null)
        {
            _logger.LogWarning("Nenhum respondente encontrado com o codigo: {LuckyNumber}", luckyNumber);
            return;
        }

        // Se ja tem telefone, ja foi processado
        if (!string.IsNullOrEmpty(respondent.PhoneNumber))
        {
            _logger.LogInformation("Pesquisa ja processada anteriormente para o codigo: {LuckyNumber}", luckyNumber);
            return;
        }

        // Atualiza os dados do respondente
        respondent.PhoneNumber = cleanPhone;
        respondent.PushName = pushName ?? "Cliente";
        respondent.WhatsappSentAt = DateTime.UtcNow;
        respondent.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Pesquisa atualizada com sucesso para o codigo {LuckyNumber} - Telefone: {Phone}", luckyNumber, cleanPhone);
    }

    private string? ExtractLuckyNumber(string message)
    {
        // Padrao 1: "Codigo: 123456" ou "Codigo:123456"
        var pattern1 = new Regex(@"Codigo:\s*(\d{6})", RegexOptions.IgnoreCase);
        var match1 = pattern1.Match(message);
        if (match1.Success)
        {
            return match1.Groups[1].Value;
        }

        // Padrao 2: Procura por 6 digitos consecutivos
        var pattern2 = new Regex(@"\b(\d{6})\b");
        var match2 = pattern2.Match(message);
        if (match2.Success)
        {
            return match2.Groups[1].Value;
        }

        return null;
    }
}
