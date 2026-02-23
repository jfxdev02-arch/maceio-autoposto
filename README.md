# Maceio Auto Posto

Sistema de Pesquisa de Satisfacao com QR Code

## Fluxo

1. Cliente escaneia QR Code e acessa a pesquisa web
2. Cliente responde as perguntas no carrossel
3. Ao final, cliente recebe codigo do sorteio
4. Cliente clica para enviar os dados via WhatsApp
5. Evolution API recebe a mensagem e atualiza o banco de dados
6. Dashboard exibe os resultados

## Quick Start

```bash
docker-compose up -d
```

## URLs

| Servico | URL |
|---------|-----|
| Landing Page | http://localhost:5001 |
| Pesquisa | http://localhost:5001/pesquisa |
| Admin | http://localhost:5001/Admin/Login |
| QR Code | http://localhost:5001/Admin/QrCode |
| Evolution API | http://localhost:8080 |

**Admin:** admin / maceio2024

## Configurar WhatsApp

```bash
# 1. Criar instancia
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

## Configuracao

### Numero do WhatsApp do Posto

Edite a variavel de ambiente `WhatsApp__Number` no `docker-compose.yml`:

```yaml
environment:
  - WhatsApp__Number=5582999999999
```

### URL da Pesquisa

Edite a variavel de ambiente `Survey__BaseUrl` no `docker-compose.yml`:

```yaml
environment:
  - Survey__BaseUrl=https://seu-dominio.com/pesquisa
```

## Stack

- ASP.NET Core 8
- PostgreSQL 15
- Evolution API v2
- Docker Compose
