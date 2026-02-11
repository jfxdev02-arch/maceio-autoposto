using System.ComponentModel.DataAnnotations;

namespace MaceioBot.Models;

public class Respondent
{
    public int Id { get; set; }
    
    [Required]
    public required string PhoneNumber { get; set; }
    
    public string? PushName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime FirstContactAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Controle de fluxo
    public string CurrentStep { get; set; } = "frequency";
    
    // Respostas
    public string? FrequencyAnswer { get; set; }
    public string? ConvenienceAnswer { get; set; }
    public string? FuelAnswer { get; set; }
    public string? RatingAnswer { get; set; }
    
    // Resultado
    public string? LuckyNumber { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Watchdog / Recovery
    public int? RetryCount { get; set; } = 0;
    public DateTime? LastRetryAt { get; set; }
}
