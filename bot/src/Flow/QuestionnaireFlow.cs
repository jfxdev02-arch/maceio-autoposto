using MaceioBot.Data;
using MaceioBot.Models;
using MaceioBot.Services;
using Microsoft.EntityFrameworkCore;

namespace MaceioBot.Flow;

public class QuestionnaireFlow
{
    private readonly AppDbContext _db;
    private readonly EvolutionApiService _evolution;
    private readonly ILogger<QuestionnaireFlow> _logger;
    private static readonly Random _random = new();

    public QuestionnaireFlow(AppDbContext db, EvolutionApiService evolution, ILogger<QuestionnaireFlow> logger)
    {
        _db = db;
        _evolution = evolution;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(string phone, string pushName, string messageText)
    {
        var respondent = await _db.Respondents.FirstOrDefaultAsync(r => r.PhoneNumber == phone);
        
        if (respondent == null)
        {
            // Novo contato - criar registro e enviar boas-vindas
            respondent = new Respondent
            {
                PhoneNumber = phone,
                PushName = pushName,
                FirstContactAt = DateTime.UtcNow,
                CurrentStep = "welcome"
            };
            _db.Respondents.Add(respondent);
            await _db.SaveChangesAsync();
            
            await SendWelcomeAsync(phone);
            return;
        }

        // Se já completou, agradecer novamente
        if (respondent.CompletedAt.HasValue)
        {
            await _evolution.SendTextMessageAsync(phone, 
                $"Olá! Você já participou da nossa pesquisa. 🎉\n\nSeu número da sorte é: *{respondent.LuckyNumber}*\n\nAguarde nossos contatos com promoções exclusivas!");
            return;
        }

        // Processar resposta baseado no passo atual
        await ProcessStepAsync(respondent, messageText);
    }

    private async Task ProcessStepAsync(Respondent respondent, string messageText)
    {
        var normalizedText = messageText.Trim().ToLowerInvariant();
        
        switch (respondent.CurrentStep)
        {
            case "welcome":
                if (normalizedText.Contains("bora") || normalizedText.Contains("começar") || normalizedText == "1")
                {
                    respondent.CurrentStep = "frequency";
                    await SendFrequencyQuestionAsync(respondent.PhoneNumber);
                }
                else
                {
                    await SendWelcomeAsync(respondent.PhoneNumber);
                }
                break;

            case "frequency":
                var freqAnswer = ParseFrequencyAnswer(normalizedText);
                if (freqAnswer != null)
                {
                    respondent.FrequencyAnswer = freqAnswer;
                    respondent.CurrentStep = "convenience";
                    await SendConvenienceQuestionAsync(respondent.PhoneNumber);
                }
                else
                {
                    await SendFrequencyQuestionAsync(respondent.PhoneNumber);
                }
                break;

            case "convenience":
                var convAnswer = ParseYesNoAnswer(normalizedText);
                if (convAnswer != null)
                {
                    respondent.ConvenienceAnswer = convAnswer;
                    respondent.CurrentStep = "fuel";
                    await SendFuelQuestionAsync(respondent.PhoneNumber);
                }
                else
                {
                    await SendConvenienceQuestionAsync(respondent.PhoneNumber);
                }
                break;

            case "fuel":
                var fuelAnswer = ParseFuelAnswer(normalizedText);
                if (fuelAnswer != null)
                {
                    respondent.FuelAnswer = fuelAnswer;
                    respondent.CurrentStep = "rating";
                    await SendRatingQuestionAsync(respondent.PhoneNumber);
                }
                else
                {
                    await SendFuelQuestionAsync(respondent.PhoneNumber);
                }
                break;

            case "rating":
                var ratingAnswer = ParseRatingAnswer(normalizedText);
                if (ratingAnswer != null)
                {
                    respondent.RatingAnswer = ratingAnswer;
                    respondent.CurrentStep = "completed";
                    respondent.CompletedAt = DateTime.UtcNow;
                    respondent.LuckyNumber = await GenerateUniqueLuckyNumberAsync();
                    await SendCompletionAsync(respondent.PhoneNumber, respondent.LuckyNumber);
                }
                else
                {
                    await SendRatingQuestionAsync(respondent.PhoneNumber);
                }
                break;
        }

        respondent.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task SendWelcomeAsync(string phone)
    {
        var text = "Olá! Bem-vindo ao canal de comunicação direto do *Maceió AutoPosto*. 🚗💨\n\n" +
                   "Responda 4 perguntas rápidas, ajude a melhorar nosso serviço e concorra a um *tanque cheio*! ⛽✨";
        
        await _evolution.SendButtonMessageAsync(phone, text, new List<string> { "Bora começar!" });
    }

    private async Task SendFrequencyQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 1*\n\nQuantas vezes por semana você abastece conosco?";
        
        await _evolution.SendButtonMessageAsync(phone, text, new List<string>
        {
            "1 vez",
            "2 vezes",
            "3 vezes ou mais"
        });
    }

    private async Task SendConvenienceQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 2*\n\nVocê utiliza nossa loja de conveniência? 🛒";
        
        await _evolution.SendButtonMessageAsync(phone, text, new List<string> { "Sim", "Não" });
    }

    private async Task SendFuelQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 3*\n\nQual combustível você utiliza com *MAIOR* frequência? ⛽";
        
        await _evolution.SendButtonMessageAsync(phone, text, new List<string>
        {
            "Gasolina Comum",
            "Gasolina Aditivada",
            "Etanol",
            "Diesel"
        });
    }

    private async Task SendRatingQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta Final*\n\nQual nota você daria ao Maceió AutoPosto? ⭐";
        
        await _evolution.SendButtonMessageAsync(phone, text, new List<string>
        {
            "5 - Excelente",
            "4 - Bom",
            "3 - Regular",
            "2 - Ruim",
            "1 - Muito Ruim"
        });
    }

    private async Task SendCompletionAsync(string phone, string luckyNumber)
    {
        var text = $"🎉 *PARTICIPAÇÃO CONFIRMADA!*\n\n" +
                   $"Seu número para o sorteio é:\n*{luckyNumber}*\n\n" +
                   $"Guarde este número. Em breve enviaremos nossos descontos e benefícios exclusivos para você!\n\n" +
                   $"*MACEIÓ AUTOPOSTO*\n_Mais que um posto_";
        
        await _evolution.SendTextMessageAsync(phone, text);
    }

    private async Task<string> GenerateUniqueLuckyNumberAsync()
    {
        string number;
        do
        {
            number = _random.Next(100000, 999999).ToString();
        }
        while (await _db.Respondents.AnyAsync(r => r.LuckyNumber == number));
        
        return number;
    }

    // Parsers de resposta
    private string? ParseFrequencyAnswer(string text)
    {
        if (text.Contains("1") && !text.Contains("2") && !text.Contains("3")) return "1 vez";
        if (text.Contains("2")) return "2 vezes";
        if (text.Contains("3") || text.Contains("mais")) return "3 vezes ou mais";
        return null;
    }

    private string? ParseYesNoAnswer(string text)
    {
        if (text.Contains("sim") || text == "s" || text == "1") return "Sim";
        if (text.Contains("não") || text.Contains("nao") || text == "n" || text == "2") return "Não";
        return null;
    }

    private string? ParseFuelAnswer(string text)
    {
        if (text.Contains("aditivada") || text == "2") return "Gasolina Aditivada";
        if (text.Contains("gasolina") || text.Contains("comum") || text == "1") return "Gasolina Comum";
        if (text.Contains("etanol") || text.Contains("alcool") || text.Contains("álcool") || text == "3") return "Etanol";
        if (text.Contains("diesel") || text == "4") return "Diesel";
        return null;
    }

    private string? ParseRatingAnswer(string text)
    {
        if (text.Contains("5") || text.Contains("excelente")) return "5 - Excelente";
        if (text.Contains("4") || text.Contains("bom")) return "4 - Bom";
        if (text.Contains("3") || text.Contains("regular")) return "3 - Regular";
        if (text.Contains("2") || text.Contains("ruim") && !text.Contains("muito")) return "2 - Ruim";
        if (text.Contains("1") || text.Contains("muito ruim") || text.Contains("péssimo")) return "1 - Muito Ruim";
        return null;
    }
}
