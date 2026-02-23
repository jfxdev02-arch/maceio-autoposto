using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaceioWeb.Data;
using MaceioWeb.Models;

namespace MaceioWeb.Controllers;

public class SurveyController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private static readonly Random _random = new();

    public SurveyController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("/pesquisa")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("/api/survey")]
    public async Task<IActionResult> Submit([FromBody] SurveyRequest request)
    {
        if (string.IsNullOrEmpty(request.FrequencyAnswer) ||
            string.IsNullOrEmpty(request.ConvenienceAnswer) ||
            string.IsNullOrEmpty(request.FuelAnswer) ||
            string.IsNullOrEmpty(request.RatingAnswer))
        {
            return BadRequest(new { error = "Todas as perguntas devem ser respondidas." });
        }

        var luckyNumber = await GenerateUniqueLuckyNumberAsync();

        var respondent = new Respondent
        {
            PhoneNumber = string.Empty, // Será preenchido quando o cliente enviar pelo WhatsApp
            FirstContactAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            CurrentStep = "completed",
            FrequencyAnswer = request.FrequencyAnswer,
            ConvenienceAnswer = request.ConvenienceAnswer,
            FuelAnswer = request.FuelAnswer,
            RatingAnswer = request.RatingAnswer,
            LuckyNumber = luckyNumber,
            Source = "web",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Respondents.Add(respondent);
        await _db.SaveChangesAsync();

        var whatsappLink = GenerateWhatsappLink(respondent);

        return Ok(new SurveyResponse
        {
            LuckyNumber = luckyNumber,
            WhatsappLink = whatsappLink
        });
    }

    private async Task<string> GenerateUniqueLuckyNumberAsync()
    {
        string number;
        do
        {
            number = _random.Next(100000, 999999).ToString();
        } while (await _db.Respondents.AnyAsync(r => r.LuckyNumber == number));
        return number;
    }

    private string GenerateWhatsappLink(Respondent respondent)
    {
        var whatsappNumber = _config["WhatsApp:Number"] ?? "5582999999999";
        var message = $@"📋 PESQUISA MACEIÓ AUTO POSTO
━━━━━━━━━━━━━━━━━━━━━
🔢 Código: {respondent.LuckyNumber}

❓ Frequência: {respondent.FrequencyAnswer}
🏪 Conveniência: {respondent.ConvenienceAnswer}
⛽ Combustível: {respondent.FuelAnswer}
⭐ Avaliação: {respondent.RatingAnswer}
━━━━━━━━━━━━━━━━━━━━━
Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";

        var encodedMessage = Uri.EscapeDataString(message);
        return $"https://wa.me/{whatsappNumber}?text={encodedMessage}";
    }
}
