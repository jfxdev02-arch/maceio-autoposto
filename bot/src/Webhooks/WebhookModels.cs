using System.Text.Json;

namespace MaceioBot.Webhooks;

public static class WebhookParser
{
    public static (string? phone, string? pushName, string? text) Parse(JsonElement root)
    {
        string? phone = null;
        string? pushName = null;
        string? text = null;

        try
        {
            // Na Evolution v2, o número real muitas vezes vem no campo 'sender' na raiz do objeto 'data'
            if (root.TryGetProperty("data", out var data))
            {
                if (data.TryGetProperty("sender", out var senderProp))
                {
                    var sender = senderProp.GetString();
                    if (!string.IsNullOrEmpty(sender))
                    {
                        phone = sender.Split('@')[0].Split(':')[0]; // Pega apenas o número antes do @ ou :
                    }
                }

                // Fallback para remoteJid se sender não estiver disponível
                if (string.IsNullOrEmpty(phone))
                {
                    if (data.TryGetProperty("key", out var key) && 
                        key.TryGetProperty("remoteJid", out var jid))
                    {
                        var jidStr = jid.GetString();
                        if (jidStr != null)
                        {
                            phone = jidStr.Split('@')[0].Split(':')[0];
                        }
                    }
                }

                // Ignorar se for do próprio bot
                if (data.TryGetProperty("key", out var keyCheck) && 
                    keyCheck.TryGetProperty("fromMe", out var fromMe) && fromMe.GetBoolean())
                {
                    return (null, null, null);
                }

                if (data.TryGetProperty("pushName", out var pushNameProp))
                {
                    pushName = pushNameProp.GetString();
                }

                if (data.TryGetProperty("message", out var message))
                {
                    if (message.TryGetProperty("conversation", out var conv))
                        text = conv.GetString();
                    else if (message.TryGetProperty("extendedTextMessage", out var extMsg) && extMsg.TryGetProperty("text", out var extText))
                        text = extText.GetString();
                    else if (message.TryGetProperty("buttonsResponseMessage", out var btnResp) && btnResp.TryGetProperty("selectedDisplayText", out var btnText))
                        text = btnText.GetString();
                    else if (message.TryGetProperty("listResponseMessage", out var listResp) && listResp.TryGetProperty("title", out var listTitle))
                        text = listTitle.GetString();
                }
            }
        }
        catch (Exception) { /* Log error */ }

        return (phone, pushName, text);
    }
}
