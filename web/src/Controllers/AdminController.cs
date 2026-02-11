using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MaceioWeb.Data;
using MaceioWeb.Models;
using System.Security.Claims;

namespace MaceioWeb.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AdminController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard");
        
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var adminUser = _config["Admin:Username"] ?? "admin";
        var adminPass = _config["Admin:Password"] ?? "maceio2024";

        if (model.Username == adminUser && model.Password == adminPass)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, model.Username),
                new(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Dashboard");
        }

        model.ErrorMessage = "Usuário ou senha inválidos";
        return View(model);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var stats = new DashboardStats();

        var respondents = await _db.Respondents.ToListAsync();
        
        stats.TotalContacts = respondents.Count;
        stats.CompletedSurveys = respondents.Count(r => r.CompletedAt.HasValue);
        stats.PendingSurveys = stats.TotalContacts - stats.CompletedSurveys;
        stats.CompletionRate = stats.TotalContacts > 0 
            ? Math.Round((double)stats.CompletedSurveys / stats.TotalContacts * 100, 1) 
            : 0;

        // Média de avaliação
        var ratings = respondents
            .Where(r => !string.IsNullOrEmpty(r.RatingAnswer))
            .Select(r => r.RatingAnswer switch
            {
                "Excelente" => 5,
                "Bom" => 4,
                "Regular" => 3,
                "Ruim" => 2,
                "Muito ruim" => 1,
                _ => 0
            })
            .Where(r => r > 0)
            .ToList();
        
        stats.AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0;

        // Distribuições
        stats.FrequencyDistribution = respondents
            .Where(r => !string.IsNullOrEmpty(r.FrequencyAnswer))
            .GroupBy(r => r.FrequencyAnswer!)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.FuelDistribution = respondents
            .Where(r => !string.IsNullOrEmpty(r.FuelAnswer))
            .GroupBy(r => r.FuelAnswer!)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.ConvenienceDistribution = respondents
            .Where(r => !string.IsNullOrEmpty(r.ConvenienceAnswer))
            .GroupBy(r => r.ConvenienceAnswer!)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.RatingDistribution = respondents
            .Where(r => !string.IsNullOrEmpty(r.RatingAnswer))
            .GroupBy(r => r.RatingAnswer!)
            .ToDictionary(g => g.Key, g => g.Count());

        // Últimos 30 dias
        var last30Days = Enumerable.Range(0, 30)
            .Select(i => DateTime.UtcNow.Date.AddDays(-i))
            .ToList();

        stats.Last30Days = last30Days.Select(date => new DailyStats
        {
            Date = date,
            NewContacts = respondents.Count(r => r.FirstContactAt.Date == date),
            Completed = respondents.Count(r => r.CompletedAt?.Date == date)
        }).OrderBy(d => d.Date).ToList();

        return View(stats);
    }

    [Authorize]
    public async Task<IActionResult> Contacts(string? search, int page = 1)
    {
        var query = _db.Respondents.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => 
                r.PhoneNumber.Contains(search) || 
                (r.PushName != null && r.PushName.Contains(search)) ||
                (r.LuckyNumber != null && r.LuckyNumber.Contains(search)));
        }

        var pageSize = 20;
        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var contacts = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Total = total;

        return View(contacts);
    }

    [Authorize]
    public async Task<IActionResult> ExportCsv()
    {
        var respondents = await _db.Respondents
            .Where(r => r.CompletedAt.HasValue)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync();

        var csv = "Telefone,Nome,Frequência,Conveniência,Combustível,Avaliação,Número Sorteio,Data Conclusão\n";
        
        foreach (var r in respondents)
        {
            csv += $"\"{r.PhoneNumber}\",\"{r.PushName}\",\"{r.FrequencyAnswer}\",\"{r.ConvenienceAnswer}\",\"{r.FuelAnswer}\",\"{r.RatingAnswer}\",\"{r.LuckyNumber}\",\"{r.CompletedAt:yyyy-MM-dd HH:mm}\"\n";
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"pesquisa-maceio-autoposto-{DateTime.Now:yyyyMMdd}.csv");
    }
}
