using MaceioBot.Data;
using MaceioBot.Models;
using MaceioBot.Services;
using Microsoft.EntityFrameworkCore;

namespace MaceioBot.Flow;

public class QuestionnaireFlow
{
    private readonly AppDbContext _db;
    private readonly AntibanService _antiban;
    private readonly ILogger<QuestionnaireFlow> _logger;
    private static readonly Random _random = new();

    public QuestionnaireFlow(AppDbContext db, AntibanService antiban, ILogger<QuestionnaireFlow> logger)
    {
        _db = db;
        _antiban = antiban;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(string phone, string pushName, string messageText)
    {
        var cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
        var respondent = await _db.Respondents.FirstOrDefaultAsync(r => r.PhoneNumber == cleanPhone);
        
        if (respondent == null)
        {
            respondent = new Respondent
            {
                PhoneNumber = cleanPhone,
                PushName = pushName,
                FirstContactAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CurrentStep = "frequency"
            };
            _db.Respondents.Add(respondent);
            await _db.SaveChangesAsync();
            
            await SendWelcomeAndFirstQuestionUnifiedAsync(cleanPhone);
            return;
        }

        if (respondent.CompletedAt.HasValue)
        {
            await _antiban.EnqueueMessageAsync(cleanPhone, 
                GetTextVariant("{Olá|Oi|Oi tudo bem?}") + "! Você já participou da nossa pesquisa. 🎉\n\nSeu número da sorte é: *" + respondent.LuckyNumber + "*\n\nAguarde nossos contatos com promoções exclusivas!");
            return;
        }

        respondent.UpdatedAt = DateTime.UtcNow;
        await ProcessStepAsync(respondent, messageText);
        await _db.SaveChangesAsync();
    }

    private async Task ProcessStepAsync(Respondent respondent, string messageText)
    {
        var normalizedText = messageText.Trim().ToLowerInvariant();
        
        switch (respondent.CurrentStep)
        {
            case "frequency":
                var freqAnswer = ParseFrequencyAnswer(normalizedText);
                if (freqAnswer != null) { respondent.FrequencyAnswer = freqAnswer; respondent.CurrentStep = "convenience"; await SendConvenienceQuestionAsync(respondent.PhoneNumber); }
                else await SendFrequencyQuestionAsync(respondent.PhoneNumber);
                break;
            case "convenience":
                var convAnswer = ParseYesNoAnswer(normalizedText);
                if (convAnswer != null) { respondent.ConvenienceAnswer = convAnswer; respondent.CurrentStep = "fuel"; await SendFuelQuestionAsync(respondent.PhoneNumber); }
                else await SendConvenienceQuestionAsync(respondent.PhoneNumber);
                break;
            case "fuel":
                var fuelAnswer = ParseFuelAnswer(normalizedText);
                if (fuelAnswer != null) { respondent.FuelAnswer = fuelAnswer; respondent.CurrentStep = "rating"; await SendRatingQuestionAsync(respondent.PhoneNumber); }
                else await SendFuelQuestionAsync(respondent.PhoneNumber);
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
                else await SendRatingQuestionAsync(respondent.PhoneNumber);
                break;
        }
    }

    private async Task SendWelcomeAndFirstQuestionUnifiedAsync(string phone)
    {
        var welcome = GetTextVariant("{Olá|Oi|Tudo bem?|Como vai?}") + "! " +
                      GetTextVariant("{Bem-vindo ao|Você está no|Iniciando atendimento no}") + " *Maceió Auto Posto*.\n\n" +
                      GetTextVariant("{Para ganhar descontos e benefícios|Para concorrer a prêmios|Para participar da nossa promoção}") + ", responda apenas *4 perguntas rápidas* e concorra a um *tanque de combustível*.";
        
        var question = "\n\n---------------------------------\n\n" +
                       "📌 *Pergunta 1*\n" + GetTextVariant("{Quantas vezes por semana você abastece?|Com que frequência você vem nos visitar?|Quantas vezes na semana você passa aqui?}") + "\n\n" +
                       "1️⃣ 1 vez\n2️⃣ 2 vezes\n3️⃣ 3 vezes ou mais";
        
        await _antiban.EnqueueMessageAsync(phone, welcome + question);
    }

    private async Task SendFrequencyQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 1*\n" + GetTextVariant("{Quantas vezes por semana você abastece?|Qual sua frequência de abastecimento?}") + "\n\n" +
                   "1️⃣ 1 vez\n2️⃣ 2 vezes\n3️⃣ 3 vezes ou mais";
        await _antiban.EnqueueMessageAsync(phone, text);
    }

    private async Task SendConvenienceQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 2*\n" + GetTextVariant("{Você utiliza nossa loja de conveniência?|Você costuma passar na nossa conveniência?|Frequenta nossa loja de conveniência?}") + "\n\n" +
                   "1️⃣ Sim\n2️⃣ Não";
        await _antiban.EnqueueMessageAsync(phone, text);
    }

    private async Task SendFuelQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta 3*\n" + GetTextVariant("{Qual combustível você utiliza com MAIOR frequência?|Qual combustível você mais usa?|O que você costuma abastecer?}") + "\n\n" +
                   "1️⃣ Gasolina comum\n2️⃣ Gasolina aditivada\n3️⃣ Etanol\n4️⃣ Diesel";
        await _antiban.EnqueueMessageAsync(phone, text);
    }

    private async Task SendRatingQuestionAsync(string phone)
    {
        var text = "📌 *Pergunta Final*\n" + GetTextVariant("{Qual nota você daria ao Maceió Auto Posto?|Como você avalia nosso posto?|Qual sua satisfação geral com a gente?}") + "\n\n" +
                   "1️⃣ Excelente\n2️⃣ Bom\n3️⃣ Regular\n4️⃣ Ruim\n5️⃣ Muito ruim";
        await _antiban.EnqueueMessageAsync(phone, text);
    }

    private async Task SendCompletionAsync(string phone, string luckyNumber)
    {
        var text = "🎉 *" + GetTextVariant("{PARTICIPAÇÃO CONFIRMADA|TUDO CERTO|CADASTRO REALIZADO}") + "!*\n🎉\n\n" +
                   "🔢 *Código do Sorteio:* " + luckyNumber + "\n\n" +
                   GetTextVariant("{Guarde este número|Salve este código|Não perca esse número}") + ".\n\n" +
                   "Agradecemos a sua disponibilidade. Em breve enviaremos nossos descontos e benefícios.\n\n" +
                   "*MACEIÓ AUTO POSTO*\n*MAIS QUE UM POSTO*";
        await _antiban.EnqueueMessageAsync(phone, text);
    }

    private string GetTextVariant(string input)
    {
        if (!input.Contains("{")) return input;
        
        var start = input.IndexOf('{');
        var end = input.IndexOf('}');
        
        if (start == -1 || end == -1) return input;
        
        var options = input.Substring(start + 1, end - start - 1).Split('|');
        var selected = options[_random.Next(options.Length)];
        
        return input.Substring(0, start) + selected + input.Substring(end + 1);
    }

    private async Task<string> GenerateUniqueLuckyNumberAsync()
    {
        string number;
        do { number = _random.Next(100000, 999999).ToString(); }
        while (await _db.Respondents.AnyAsync(r => r.LuckyNumber == number));
        return number;
    }

    private string? ParseFrequencyAnswer(string text)
    {
        if (text == "1 vez" || text == "1") return "1 vez";
        if (text == "2 vezes" || text == "2") return "2 vezes";
        if (text == "3 vezes ou mais" || text.Contains("3") || text.Contains("mais")) return "3 vezes ou mais";
        return null;
    }

    private string? ParseYesNoAnswer(string text)
    {
        if (text == "sim" || text == "s" || text == "1") return "Sim";
        if (text == "não" || text == "nao" || text == "n" || text == "2") return "Não";
        return null;
    }

    private string? ParseFuelAnswer(string text)
    {
        if (text == "gasolina aditivada" || text == "2") return "Gasolina aditivada";
        if (text == "gasolina comum" || text.Contains("gasolina") || text.Contains("comum") || text == "1") return "Gasolina comum";
        if (text == "etanol" || text.Contains("alcool") || text.Contains("álcool") || text == "3") return "Etanol";
        if (text == "diesel" || text == "4") return "Diesel";
        return null;
    }

    private string? ParseRatingAnswer(string text)
    {
        if (text == "excelente" || text == "1") return "Excelente";
        if (text == "bom" || text == "2") return "Bom";
        if (text == "regular" || text == "3") return "Regular";
        if (text == "ruim" || text == "4") return "Ruim";
        if (text == "muito ruim" || text.Contains("péssimo") || text == "5") return "Muito ruim";
        return null;
    }
}
