# Maceió Auto Posto

Bot WhatsApp + Landing Page + Dashboard Admin

## Quick Start

```bash
docker-compose up -d
```

## URLs

| Serviço | URL |
|---------|-----|
| Landing Page | http://localhost:5001 |
| Admin | http://localhost:5001/Admin/Login |
| Evolution API | http://localhost:8080 |

**Admin:** admin / maceio2024

## Configurar WhatsApp

```bash
# 1. Criar instância
curl -X POST http://localhost:8080/instance/create \
  -H "apikey: maceio-autoposto-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"instanceName":"maceio-whatsapp","integration":"WHATSAPP-BAILEYS","qrcode":true}'

# 2. Obter QR Code
curl http://localhost:8080/instance/connect/maceio-whatsapp \
  -H "apikey: maceio-autoposto-key-2024"

# 3. Configurar Webhook
curl -X POST http://localhost:8080/webhook/set/maceio-whatsapp \
  -H "apikey: maceio-autoposto-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"webhook":{"enabled":true,"url":"http://bot:5000/webhook/evolution","events":["MESSAGES_UPSERT"]}}'
```

## Stack

- ASP.NET Core 8
- PostgreSQL 15
- Evolution API v2
- Docker Compose
