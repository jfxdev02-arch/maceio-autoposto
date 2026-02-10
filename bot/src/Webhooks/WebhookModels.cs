using System.Text.Json;

namespace MaceioBot.Webhooks;

public class EvolutionWebhookPayload
{
    public string? Event { get; set; }
    public string? Instance { get; set; }
    public WebhookData? Data { get; set; }
}

public class WebhookData
{
    public WebhookKey? Key { get; set; }
    public string? PushName { get; set; }
    public WebhookMessage? Message { get; set; }
}

public class WebhookKey
{
    public string? RemoteJid { get; set; }
    public bool FromMe { get; set; }
    public string? Id { get; set; }
}

public class WebhookMessage
{
    public string? Conversation { get; set; }
    public ExtendedTextMessage? ExtendedTextMessage { get; set; }
    public ButtonsResponseMessage? ButtonsResponseMessage { get; set; }
    public ListResponseMessage? ListResponseMessage { get; set; }
}

public class ExtendedTextMessage
{
    public string? Text { get; set; }
}

public class ButtonsResponseMessage
{
    public string? SelectedButtonId { get; set; }
    public string? SelectedDisplayText { get; set; }
}

public class ListResponseMessage
{
    public string? Title { get; set; }
    public SingleSelectReply? SingleSelectReply { get; set; }
}

public class SingleSelectReply
{
    public string? SelectedRowId { get; set; }
}

public static class WebhookParser
{
    public static (string? phone, string? pushName, string? text) Parse(JsonElement root)
    {
        string? phone = null;
        string? pushName = null;
        string? text = null;

        try
        {
            if (root.TryGetProperty("data", out var data))
            {
                // Extrair número do telefone
                if (data.TryGetProperty("key", out var key) && 
                    key.TryGetProperty("remoteJid", out var jid))
                {
                    var jidStr = jid.GetString();
                    if (jidStr != null)
                    {
                        // Formato: 5582999999999@s.whatsapp.net
                        phone = jidStr.Split('@')[0];
                    }
                    
                    // Ignorar mensagens enviadas pelo próprio bot
                    if (key.TryGetProperty("fromMe", out var fromMe) && fromMe.GetBoolean())
                    {
                        return (null, null, null);
                    }
                }

                // Extrair nome do contato
                if (data.TryGetProperty("pushName", out var pushNameProp))
                {
                    pushName = pushNameProp.GetString();
                }

                // Extrair texto da mensagem (vários formatos possíveis)
                if (data.TryGetProperty("message", out var message))
                {
                    // Mensagem de texto simples
                    if (message.TryGetProperty("conversation", out var conv))
                    {
                        text = conv.GetString();
                    }
                    // Mensagem de texto estendida
                    else if (message.TryGetProperty("extendedTextMessage", out var extMsg) &&
                             extMsg.TryGetProperty("text", out var extText))
                    {
                        text = extText.GetString();
                    }
                    // Resposta de botão
                    else if (message.TryGetProperty("buttonsResponseMessage", out var btnResp) &&
                             btnResp.TryGetProperty("selectedDisplayText", out var btnText))
                    {
                        text = btnText.GetString();
                    }
                    // Resposta de lista
                    else if (message.TryGetProperty("listResponseMessage", out var listResp))
                    {
                        if (listResp.TryGetProperty("title", out var listTitle))
                        {
                            text = listTitle.GetString();
                        }
                        else if (listResp.TryGetProperty("singleSelectReply", out var selectReply) &&
                                 selectReply.TryGetProperty("selectedRowId", out var rowId))
                        {
                            text = rowId.GetString();
                        }
                    }
                    // Interactive button response (Evolution v2)
                    else if (message.TryGetProperty("interactiveResponseMessage", out var interResp))
                    {
                        if (interResp.TryGetProperty("nativeFlowResponseMessage", out var nativeFlow) &&
                            nativeFlow.TryGetProperty("paramsJson", out var paramsJson))
                        {
                            var paramsStr = paramsJson.GetString();
                            if (!string.IsNullOrEmpty(paramsStr))
                            {
                                using var paramsDoc = JsonDocument.Parse(paramsStr);
                                if (paramsDoc.RootElement.TryGetProperty("id", out var idProp))
                                {
                                    text = idProp.GetString();
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Log error if needed
        }

        return (phone, pushName, text);
    }
}
