using System.Collections.Concurrent;
using System.Threading.Channels;
using MaceioBot.Services;

namespace MaceioBot.Services;

public class OutgoingMessage
{
    public required string Phone { get; set; }
    public required string Text { get; set; }
}

public class AntibanService : BackgroundService
{
    private readonly EvolutionApiService _evolution;
    private readonly ILogger<AntibanService> _logger;
    private readonly Channel<OutgoingMessage> _messageChannel;
    private static readonly Random _random = new();
    
    public AntibanService(EvolutionApiService evolution, ILogger<AntibanService> logger)
    {
        _evolution = evolution;
        _logger = logger;
        // Fila ilimitada para garantir que não percamos mensagens durante picos
        _messageChannel = Channel.CreateUnbounded<OutgoingMessage>();
    }

    public async Task EnqueueMessageAsync(string phone, string text)
    {
        await _messageChannel.Writer.WriteAsync(new OutgoingMessage { Phone = phone, Text = text });
        _logger.LogInformation("Mensagem para {Phone} adicionada à fila global.", phone);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processador de Fila Global Antiban iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await _messageChannel.Reader.WaitToReadAsync(stoppingToken))
                {
                    while (_messageChannel.Reader.TryRead(out var msg))
                    {
                        // 1. Calcular Delay baseado no horário
                        var delayMs = GetDynamicDelay();
                        
                        _logger.LogInformation("Fila Global: Aguardando {Delay}ms antes do próximo envio (Horário: {Time}).", 
                            delayMs, DateTime.Now.ToString("HH:mm"));
                        
                        await Task.Delay(delayMs, stoppingToken);

                        // 2. Enviar via Evolution
                        await _evolution.SendTextMessageAsync(msg.Phone, msg.Text);
                        
                        _logger.LogInformation("Mensagem enviada para {Phone} via Fila Global.", msg.Phone);
                    }
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar fila global de mensagens.");
                await Task.Delay(5000, stoppingToken); // Espera um pouco em caso de erro persistente
            }
        }
    }

    private int GetDynamicDelay()
    {
        // Obter hora atual no Brasil (GMT-3)
        var brTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
        
        int minDelay, maxDelay;

        // Se for entre 22h e 08h
        if (brTime.Hour >= 22 || brTime.Hour < 8)
        {
            // Night Mode: Mais lento (15 a 30 segundos entre qualquer mensagem)
            minDelay = 15000;
            maxDelay = 30001;
        }
        else
        {
            // Business Hours: Standard (5 a 10 segundos entre qualquer mensagem)
            minDelay = 5000;
            maxDelay = 10001;
        }

        return _random.Next(minDelay, maxDelay);
    }

    public object GetStats() => new { PendingMessages = _messageChannel.Reader.Count };
}
