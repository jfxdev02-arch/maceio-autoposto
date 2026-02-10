namespace MaceioBot.Models;

public class Respondent
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PushName { get; set; }
    public DateTime FirstContactAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CurrentStep { get; set; } = "welcome";
    
    // Respostas
    public string? FrequencyAnswer { get; set; }      // 1 vez, 2 vezes, 3+
    public string? ConvenienceAnswer { get; set; }    // Sim, Não
    public string? FuelAnswer { get; set; }           // Gasolina Comum, Aditivada, Etanol, Diesel
    public string? RatingAnswer { get; set; }         // 1-5
    
    // Sorteio
    public string? LuckyNumber { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
