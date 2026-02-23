# Maceio Auto Posto

Sistema de Pesquisa de Satisfacao com QR Code

## Fluxo

1. Cliente escaneia QR Code e acessa a pesquisa web
2. Cliente responde as perguntas no carrossel
3. Ao final, cliente recebe codigo do sorteio
4. Cliente clica para enviar os dados via WhatsApp
5. Evolution API recebe a mensagem e atualiza o banco de dados
6. Dashboard exibe os resultados

## Deploy em Producao

### 1. Clone o repositorio
```bash
git clone https://github.com/jfxdev02-arch/maceio-autoposto.git
cd maceio-autoposto
```

### 2. Configure as variaveis de ambiente
```bash
cp .env.example .env
```

Edite o arquivo `.env` com suas configuracoes:
```
SERVER_URL=http://seu-ip-ou-dominio:8080
EVOLUTION_API_KEY=maceio-autoposto-key-2024
SURVEY_URL=http://seu-ip-ou-dominio:5001/pesquisa
```

### 3. Inicie os containers
```bash
docker-compose up -d --build
```

### 4. Configure o WhatsApp

Acesse a Evolution API para conectar o WhatsApp Business:

```bash
# Criar instancia
curl -X POST http://localhost:8080/instance/create \
  -H "apikey: maceio-autoposto-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"instanceName":"maceio-whatsapp","integration":"WHATSAPP-BAILEYS","qrcode":true}'

# Obter QR Code para conectar
curl http://localhost:8080/instance/connect/maceio-whatsapp \
  -H "apikey: maceio-autoposto-key-2024"

# Configurar Webhook
curl -X POST http://localhost:8080/webhook/set/maceio-whatsapp \
  -H "apikey: maceio-autoposto-key-2024" \
  -H "Content-Type: application/json" \
  -d '{"webhook":{"enabled":true,"url":"http://bot:5000/webhook/evolution","events":["MESSAGES_UPSERT"]}}'
```

### 5. Configure o numero do WhatsApp no Admin

1. Acesse `http://seu-ip:5001/Admin/Login`
2. Login: **admin** / Senha: **maceio2024**
3. Va em **Configuracoes**
4. Digite o numero do WhatsApp Business (ex: 5582999999999)
5. Clique em **Salvar**

### 6. Gere o QR Code da Pesquisa

1. No Admin, va em **QR Code**
2. Baixe o PNG ou PDF
3. Imprima e cole no posto

## URLs

| Servico | URL |
|---------|-----|
| Landing Page | http://seu-ip:5001 |
| Pesquisa | http://seu-ip:5001/pesquisa |
| Admin | http://seu-ip:5001/Admin/Login |
| Evolution API | http://seu-ip:8080 |

## Stack

- ASP.NET Core 9
- PostgreSQL 15
- Evolution API v2
- Docker Compose
