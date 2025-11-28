# Mock Servers - PayFlow

Este documento descreve os mock servers criados para simular os provedores de pagamento FastPay e SecurePay, permitindo testes completos do sistema PayFlow sem depender de APIs externas.

## Visão Geral

Os mock servers são APIs ASP.NET Core Minimal API que simulam o comportamento dos provedores de pagamento reais. Eles permitem testar todos os cenários do sistema, incluindo sucesso, falha, timeout e fallback.

## Serviços Disponíveis

### MockFastPay
- **Porta**: 8082 (host) / 8080 (container)
- **Swagger UI**: http://localhost:8082/swagger
- **Endpoint**: `POST /payments`
- **Formato de Request**: FastPay
- **Formato de Response**: FastPay

### MockSecurePay
- **Porta**: 8083 (host) / 8080 (container)
- **Swagger UI**: http://localhost:8083/swagger
- **Endpoint**: `POST /payments`
- **Formato de Request**: SecurePay
- **Formato of Response**: SecurePay

## Modos de Operação

Os mock servers suportam diferentes modos de operação controlados via variável de ambiente `MOCK_MODE` ou endpoints de controle:

### Modos Disponíveis

- **`success`** (padrão): Sempre retorna sucesso com status "approved"/"success"
- **`failure`**: Retorna erro HTTP 500 (Internal Server Error)
- **`rejected`**: Retorna sucesso HTTP 200 mas com status "rejected"/"failure"
- **`timeout`**: Demora 60 segundos para responder (testa timeout do HttpClient)
- **`unavailable`**: Retorna HTTP 503 (Service Unavailable)

## Controle de Modos

### Via Variável de Ambiente

No `docker-compose.yml`:

```yaml
services:
  mock-fastpay:
    environment:
      - MOCK_MODE=success  # ou failure, rejected, timeout, unavailable
```

### Via Endpoints de Controle

#### Definir Modo
```http
POST /mock/set-mode
Content-Type: application/json

{
  "mode": "success"
}
```

**Resposta:**
```json
{
  "mode": "success",
  "message": "Mode updated successfully"
}
```

#### Verificar Status
```http
GET /mock/status
```

**Resposta:**
```json
{
  "mode": "success",
  "availableModes": ["success", "failure", "rejected", "timeout", "unavailable"]
}
```

## Formato de Requisições e Respostas

### MockFastPay

**Request:**
```json
{
  "transaction_amount": 120.50,
  "currency": "BRL",
  "payer": {
    "email": "cliente@teste.com"
  },
  "installments": 1,
  "description": "Compra via FastPay"
}
```

**Response (success):**
```json
{
  "id": "FP-884512",
  "status": "approved",
  "status_detail": "Pagamento aprovado"
}
```

**Response (rejected):**
```json
{
  "id": "FP-884512",
  "status": "rejected",
  "status_detail": "Pagamento rejeitado"
}
```

### MockSecurePay

**Request:**
```json
{
  "amount_cents": 12050,
  "currency_code": "BRL",
  "client_reference": "ORD-20251022"
}
```

**Response (success):**
```json
{
  "transaction_id": "SP-19283",
  "result": "success"
}
```

**Response (rejected):**
```json
{
  "transaction_id": "SP-19283",
  "result": "failure"
}
```

## Execução com Docker Compose

Os mock servers são iniciados automaticamente com o Docker Compose:

```bash
docker-compose up
```

Isso inicia:
- `mock-fastpay` na porta 8082
- `mock-securepay` na porta 8083
- `payflow-api` na porta 5000 (configurada para usar os mocks)

## Execução Local (Desenvolvimento)

Para executar os mocks localmente sem Docker:

### MockFastPay
```bash
cd MockFastPay
dotnet run
```

Acesse: http://localhost:8082/swagger

### MockSecurePay
```bash
cd MockSecurePay
dotnet run
```

Acesse: http://localhost:8083/swagger

## Cenários de Teste

### 1. Sucesso Normal
- Ambos mocks em modo `success`
- Resultado: Pagamento aprovado com provider correto

### 2. Fallback - FastPay Falha
- FastPay em modo `failure`
- SecurePay em modo `success`
- Resultado: Sistema usa SecurePay como fallback

### 3. Fallback - SecurePay Falha
- SecurePay em modo `failure`
- FastPay em modo `success`
- Resultado: Sistema usa FastPay como fallback

### 4. Ambos Falham
- Ambos mocks em modo `failure`
- Resultado: Sistema retorna status "rejected"

### 5. Timeout
- Mock em modo `timeout`
- Resultado: HttpClient timeout, sistema tenta fallback

### 6. Status Rejeitado
- Mock em modo `rejected`
- Resultado: Sistema retorna status "rejected" mesmo com HTTP 200

## Scripts de Teste

Scripts PowerShell estão disponíveis na pasta `tests/`:

- **`test-all-scenarios.ps1`**: Testa todos os cenários automaticamente
- **`test-fallback.ps1`**: Testa especificamente o mecanismo de fallback
- **`test-fees.ps1`**: Valida cálculos de taxas

### Executar Testes

```powershell
# Todos os cenários
.\tests\test-all-scenarios.ps1

# Apenas fallback
.\tests\test-fallback.ps1

# Apenas taxas
.\tests\test-fees.ps1
```

## Exemplos de Uso

### Testar Fallback via PowerShell

```powershell
# Configurar FastPay para falhar
$body = @{ Mode = "failure" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8082/mock/set-mode" -Method Post -Body $body -ContentType "application/json"

# Fazer pagamento (deve usar SecurePay como fallback)
$payment = @{
    amount = 50.00
    currency = "BRL"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/payments" -Method Post -Body $payment -ContentType "application/json"
```

### Testar via Swagger

1. Acesse http://localhost:8082/swagger (MockFastPay)
2. Use o endpoint `POST /mock/set-mode` para alterar o modo
3. Use o endpoint `POST /payments` para testar o pagamento
4. Verifique a resposta conforme o modo configurado

## Benefícios

1. **Testabilidade Completa**: Permite testar todos os cenários sem APIs externas
2. **Controle Total**: Modos podem ser alterados dinamicamente durante testes
3. **Isolamento**: Testes não dependem de serviços externos
4. **Reprodutibilidade**: Mesmos cenários podem ser reproduzidos facilmente
5. **Desenvolvimento**: Facilita desenvolvimento sem depender de providers reais

## Arquitetura

```
┌─────────────┐
│ PayFlow API │
└──────┬──────┘
       │
       ├──────────────┬──────────────┐
       │              │              │
       ▼              ▼              ▼
┌──────────┐    ┌──────────┐    ┌──────────┐
│MockFastPay│   │MockSecurePay│  │  Outros  │
│  :8082   │    │   :8083   │    │ Providers│
└──────────┘    └──────────┘    └──────────┘
```

## Configuração no Docker Compose

```yaml
services:
  mock-fastpay:
    build:
      context: .
      dockerfile: MockFastPay/Dockerfile
    ports:
      - "8082:8080"
    environment:
      - MOCK_MODE=success
    networks:
      - payflow-network

  mock-securepay:
    build:
      context: .
      dockerfile: MockSecurePay/Dockerfile
    ports:
      - "8083:8080"
    environment:
      - MOCK_MODE=success
    networks:
      - payflow-network
```

## Troubleshooting

### Mock não responde
- Verifique se o container está rodando: `docker ps`
- Verifique os logs: `docker logs mock-fastpay` ou `docker logs mock-securepay`
- Verifique se a porta está correta

### Modo não muda
- Use o endpoint `/mock/status` para verificar o modo atual
- Certifique-se de usar o endpoint correto (`/mock/set-mode`)
- Verifique se o JSON está correto

### Timeout não funciona
- O modo `timeout` demora 60 segundos
- O HttpClient da API principal tem timeout de 30 segundos
- Isso permite testar o comportamento de timeout

## Próximos Passos

Para aprender sobre testes unitários e como testar o código diretamente, consulte a [documentação de testes unitários](DOC_TESTES_UNITARIOS.md).

