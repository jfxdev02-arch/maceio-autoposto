namespace MaceioWeb.Models;

public class Respondent
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PushName { get; set; }
    public DateTime FirstContactAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CurrentStep { get; set; } = "welcome";
    
    public string? FrequencyAnswer { get; set; }
    public string? ConvenienceAnswer { get; set; }
    public string? FuelAnswer { get; set; }
    public string? RatingAnswer { get; set; }
    
    public string? LuckyNumber { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Novos campos para o fluxo web
    public string Source { get; set; } = "web"; // "web" ou "whatsapp"
    public DateTime? WhatsappSentAt { get; set; } // Quando o cliente enviou pelo WhatsApp
}

public class SurveyRequest
{
    public string FrequencyAnswer { get; set; } = string.Empty;
    public string ConvenienceAnswer { get; set; } = string.Empty;
    public string FuelAnswer { get; set; } = string.Empty;
    public string RatingAnswer { get; set; } = string.Empty;
}

public class SurveyResponse
{
    public string LuckyNumber { get; set; } = string.Empty;
    public string WhatsappLink { get; set; } = string.Empty;
}

public class DashboardStats
{
    public int TotalContacts { get; set; }
    public int CompletedSurveys { get; set; }
    public int PendingSurveys { get; set; }
    public double CompletionRate { get; set; }
    public double AverageRating { get; set; }
    
    public Dictionary<string, int> FrequencyDistribution { get; set; } = new();
    public Dictionary<string, int> FuelDistribution { get; set; } = new();
    public Dictionary<string, int> ConvenienceDistribution { get; set; } = new();
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    
    public List<DailyStats> Last30Days { get; set; } = new();
}

public class DailyStats
{
    public DateTime Date { get; set; }
    public int NewContacts { get; set; }
    public int Completed { get; set; }
}

public class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}
