# PayFlow - Sistema de Pagamentos

Sistema de gateway de pagamentos desenvolvido em C# (.NET 9) que permite integração com múltiplos provedores de pagamento de forma transparente e flexível.

## Documentação

Este projeto possui documentação completa organizada em guias específicos:

1. **[Guia de Execução com Docker](DOC_DOCKER.md)** - Aprenda como subir e gerenciar os containers Docker
   - Como executar o sistema com Docker Compose
   - Como verificar status dos containers
   - Solução de problemas comuns

2. **[Guia de Testes no Swagger](DOC_TESTES_SWAGGER.md)** - Aprenda como testar a API usando Swagger UI
   - Como acessar e usar o Swagger
   - Testes passo a passo de cada funcionalidade
   - Interpretação de respostas

3. **[Documentação dos Mock Servers](MOCK_SERVERS.md)** - Entenda os servidores mock e como controlá-los
   - Modos de operação dos mocks
   - Como configurar cenários de teste
   - Controle via endpoints

4. **[Guia de Testes Unitários](DOC_TESTES_UNITARIOS.md)** - Aprenda como executar e criar testes unitários
   - Estrutura de testes
   - Como criar mocks
   - Boas práticas
   - Testes de Docker e containers
   - Testes de API e endpoints
   - Testes de fallback
   - Testes unitários automatizados

## Arquitetura

O sistema foi desenvolvido seguindo o padrão **Strategy Pattern** para abstrair os diferentes provedores de pagamento, permitindo fácil extensão e manutenção.

### Estrutura de Camadas

```
PayFlow/
├── PayFlow.API/              # Camada de apresentação (Minimal APIs)
├── PayFlow.Domain/           # Modelos de domínio e interfaces
│   ├── Models/               # DTOs e entidades
│   ├── Interfaces/          # Contratos dos provedores
│   └── Constants/           # Constantes do sistema
├── PayFlow.Services/         # Lógica de negócio
│   └── PaymentService.cs    # Orquestração de pagamentos
└── PayFlow.Providers/        # Implementações dos provedores
    ├── FastPayProvider.cs    # Integração com FastPay
    ├── SecurePayProvider.cs  # Integração com SecurePay
    └── HttpClientWrapper.cs # Wrapper para chamadas HTTP
```

### Fluxo de Execução

1. Cliente envia requisição POST /payments
2. Endpoint valida entrada e delega para PaymentService
3. PaymentService determina provedor baseado no valor:
   - Valor < R$ 100,00 → FastPay
   - Valor ≥ R$ 100,00 → SecurePay
4. Tenta processar com provedor primário
5. Se falhar → Tenta com provedor secundário (fallback)
6. Calcula taxas e valores líquidos
7. Retorna resposta padronizada

## Provedores de Pagamento

### FastPay
- **Uso**: Valores inferiores a R$ 100,00
- **Taxa**: 3,49% sobre o valor
- **Request Format**:
  ```json
  {
    "transaction_amount": 120.50,
    "currency": "BRL",
    "payer": {"email": "cliente@teste.com"},
    "installments": 1,
    "description": "Compra via FastPay"
  }
  ```
- **Response Format**:
  ```json
  {
    "id": "FP-884512",
    "status": "approved",
    "status_detail": "Pagamento aprovado"
  }
  ```

### SecurePay
- **Uso**: Valores iguais ou superiores a R$ 100,00
- **Taxa**: 2,99% + R$ 0,40 fixos
- **Request Format**:
  ```json
  {
    "amount_cents": 12050,
    "currency_code": "BRL",
    "client_reference": "ORD-20251022"
  }
  ```
- **Response Format**:
  ```json
  {
    "transaction_id": "SP-19283",
    "result": "success"
  }
  ```

## API Endpoints

### POST /payments

Processa um pagamento através do provedor apropriado.

**Request:**
```json
{
  "amount": 120.50,
  "currency": "BRL"
}
```

**Response (200 OK):**
```json
{
  "id": 2,
  "externalId": "SP-19283",
  "status": "approved",
  "provider": "SecurePay",
  "grossAmount": 120.50,
  "fee": 4.01,
  "netAmount": 116.49
}
```

**Campos da Resposta:**
- `id`: ID interno do pagamento (sequencial)
- `externalId`: ID retornado pelo provedor externo
- `status`: Status do pagamento ("approved" ou "rejected")
- `provider`: Nome do provedor utilizado ("FastPay" ou "SecurePay")
- `grossAmount`: Valor bruto da transação
- `fee`: Taxa calculada
- `netAmount`: Valor líquido (bruto - taxa)

**Validações:**
- `amount`: Deve estar entre 0.01 e 1.000.000,00
- `currency`: Obrigatório, deve ser "BRL"

## Requisitos

- .NET 9.0 SDK
- Docker e Docker Compose (para execução via container)

## Execução

Para instruções detalhadas sobre como subir os containers Docker, consulte o [Guia de Execução com Docker](DOC_DOCKER.md).

### Opção 1: Docker Compose (Recomendado)

1. Clone o repositório:
   ```bash
   git clone <repository-url>
   cd SOUE_Desafio_Tecnico_DevOps
   ```

2. Execute com Docker Compose:
   ```bash
   docker-compose up --build
   ```

3. Os serviços estarão disponíveis em:
   - **PayFlow API**: `http://localhost:5000/swagger`
   - **MockFastPay**: `http://localhost:8082/swagger`
   - **MockSecurePay**: `http://localhost:8083/swagger`

4. A API principal estará configurada para usar os mock servers automaticamente

### Opção 2: Execução Local

1. Restaure as dependências:
   ```bash
   dotnet restore
   ```

2. Execute a aplicação:
   ```bash
   cd PayFlow.API
   dotnet run
   ```

3. A API estará disponível em `https://localhost:5001` ou `http://localhost:5000`

### Configuração de Provedores

As URLs dos provedores podem ser configuradas via:

1. **appsettings.json**:
   ```json
   {
     "PaymentProviders": {
       "FastPay": {
         "BaseUrl": "https://api.fastpay.com"
       },
       "SecurePay": {
         "BaseUrl": "https://api.securepay.com"
       }
     }
   }
   ```

2. **Variáveis de Ambiente** (Docker Compose):
   ```yaml
   environment:
     - PaymentProviders__FastPay__BaseUrl=https://api.fastpay.com
     - PaymentProviders__SecurePay__BaseUrl=https://api.securepay.com
   ```

## Como Testar

### Teste 1: Pagamento com FastPay (Valor < R$ 100)

**Comando curl:**
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 50.00,
    "currency": "BRL"
  }'
```

**Comando PowerShell:**
```powershell
$body = @{
    amount = 50.00
    currency = "BRL"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/payments" -Method Post -Body $body -ContentType "application/json"
```

**Resposta esperada:**
```json
{
  "id": 1,
  "externalId": "FP-xxxxx",
  "status": "approved",
  "provider": "FastPay",
  "grossAmount": 50.00,
  "fee": 1.745,
  "netAmount": 48.255
}
```

**Validações:**
- `provider` deve ser "FastPay"
- `fee` deve ser 3.49% de 50.00 = 1.745
- `netAmount` = `grossAmount` - `fee`

### Teste 2: Pagamento com SecurePay (Valor ≥ R$ 100)

**Comando curl:**
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 120.50,
    "currency": "BRL"
  }'
```

**Comando PowerShell:**
```powershell
$body = @{
    amount = 120.50
    currency = "BRL"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/payments" -Method Post -Body $body -ContentType "application/json"
```

**Resposta esperada:**
```json
{
  "id": 2,
  "externalId": "SP-xxxxx",
  "status": "approved",
  "provider": "SecurePay",
  "grossAmount": 120.50,
  "fee": 4.00295,
  "netAmount": 116.49705
}
```

**Validações:**
- `provider` deve ser "SecurePay"
- `fee` deve ser (120.50 * 2.99%) + 0.40 = 4.00295
- `netAmount` = `grossAmount` - `fee`

### Teste 3: Sistema de Fallback

#### 3.1 Configurar FastPay para falhar

**Comando curl:**
```bash
curl -X POST http://localhost:8082/mock/set-mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "failure"}'
```

**Comando PowerShell:**
```powershell
$body = @{ Mode = "failure" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8082/mock/set-mode" -Method Post -Body $body -ContentType "application/json"
```

#### 3.2 Fazer pagamento que usaria FastPay

**Comando:**
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 50.00,
    "currency": "BRL"
  }'
```

**Resultado esperado:**
- `provider` deve ser "SecurePay" (fallback funcionou)
- `status` deve ser "approved"

#### 3.3 Restaurar FastPay para sucesso

```bash
curl -X POST http://localhost:8082/mock/set-mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "success"}'
```

### Teste 4: Validação de Cálculo de Taxas

#### FastPay (3.49%)
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{"amount": 100.00, "currency": "BRL"}'
```

**Validação:** `fee` deve ser 3.49 (100.00 * 0.0349)

#### SecurePay (2.99% + R$ 0.40)
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{"amount": 200.00, "currency": "BRL"}'
```

**Validação:** `fee` deve ser 5.98 + 0.40 = 6.38

### Teste 5: Validação de Entrada

#### Valor inválido (negativo)
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{"amount": -10.00, "currency": "BRL"}'
```

**Resposta esperada:** 400 Bad Request com mensagem de erro

#### Moeda inválida
```bash
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -d '{"amount": 100.00, "currency": "USD"}'
```

**Resposta esperada:** 400 Bad Request com mensagem de erro

### Teste 6: Verificar Status dos Mocks

**MockFastPay:**
```bash
curl http://localhost:8082/mock/status
```

**MockSecurePay:**
```bash
curl http://localhost:8083/mock/status
```

## Mock Servers

O projeto inclui mock servers completos para FastPay e SecurePay, permitindo testar todos os cenários do sistema sem depender de APIs externas.

### Serviços Disponíveis

- **MockFastPay**: http://localhost:8082/swagger
- **MockSecurePay**: http://localhost:8083/swagger

### Execução com Docker Compose

Os mock servers são iniciados automaticamente com o Docker Compose:

```bash
docker-compose up
```

Isso inicia:
- `mock-fastpay` na porta 8082
- `mock-securepay` na porta 8083
- `payflow-api` na porta 5000 (configurada para usar os mocks)

### Controle de Cenários

Os mock servers suportam diferentes modos de operação:

- **`success`**: Sempre retorna sucesso
- **`failure`**: Retorna erro HTTP 500
- **`rejected`**: Retorna status "rejected"/"failure"
- **`timeout`**: Demora 60 segundos (testa timeout)
- **`unavailable`**: Retorna HTTP 503

**Controle via endpoint:**
```bash
# Alterar modo do MockFastPay
curl -X POST http://localhost:8082/mock/set-mode \
  -H "Content-Type: application/json" \
  -d '{"mode": "failure"}'

# Verificar status
curl http://localhost:8082/mock/status
```

### Documentação Completa

Para mais detalhes sobre os mock servers, consulte [MOCK_SERVERS.md](MOCK_SERVERS.md).

## Funcionalidades

### Seleção Automática de Provedor
- Valores < R$ 100,00 → FastPay
- Valores ≥ R$ 100,00 → SecurePay

### Sistema de Fallback
- Se o provedor primário estiver indisponível, o sistema automaticamente tenta o provedor alternativo
- Garante alta disponibilidade mesmo em caso de falha de um provedor

### Cálculo Automático de Taxas
- **FastPay**: 3,49% do valor
- **SecurePay**: 2,99% + R$ 0,40 fixos
- Valores líquidos calculados automaticamente

## Padrões de Design Utilizados

- **Strategy Pattern**: Abstração dos provedores de pagamento através da interface `IPaymentProvider`
- **Dependency Injection**: Injeção de dependências via ASP.NET Core DI Container
- **Separação de Responsabilidades**: Separação entre camadas de domínio, serviços e infraestrutura

## Licença

Este projeto foi desenvolvido como parte de um desafio técnico.
