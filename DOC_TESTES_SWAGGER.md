# Guia de Testes no Swagger

Este guia explica como testar a API PayFlow usando a interface Swagger UI.

## Acessar o Swagger

1. Certifique-se de que os containers Docker estão rodando (consulte [Guia de Execução com Docker](DOC_DOCKER.md))

2. Abra o navegador e acesse:
   ```
   http://localhost:5000/swagger
   ```

## Interface do Swagger

A interface do Swagger exibe:
- Lista de endpoints disponíveis
- Descrição de cada endpoint
- Modelos de requisição e resposta
- Botão "Try it out" para testar endpoints

## Teste 1: Pagamento com FastPay

### Passo a Passo

1. Na página do Swagger, encontre o endpoint **POST /payments**
2. Clique no endpoint para expandir
3. Clique no botão **"Try it out"**
4. No campo **Request body**, cole o seguinte JSON:
   ```json
   {
     "amount": 50.00,
     "currency": "BRL"
   }
   ```
5. Clique no botão **"Execute"** (verde)

### Resultado Esperado

Você deve receber uma resposta 200 OK com o seguinte formato:

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

### Validações

- `provider` deve ser "FastPay" (porque 50.00 < 100.00)
- `status` deve ser "approved"
- `fee` deve ser 1.745 (3.49% de 50.00)
- `netAmount` = `grossAmount` - `fee`

## Teste 2: Pagamento com SecurePay

### Passo a Passo

1. No mesmo endpoint **POST /payments**, clique em **"Try it out"** novamente
2. No campo **Request body**, cole:
   ```json
   {
     "amount": 120.50,
     "currency": "BRL"
   }
   ```
3. Clique em **"Execute"**

### Resultado Esperado

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

### Validações

- `provider` deve ser "SecurePay" (porque 120.50 >= 100.00)
- `fee` deve ser 4.00295 ((120.50 * 2.99%) + 0.40)
- `netAmount` = `grossAmount` - `fee`

## Teste 3: Validação de Entrada

### Teste de Valor Inválido

1. No endpoint **POST /payments**, use:
   ```json
   {
     "amount": -10.00,
     "currency": "BRL"
   }
   ```

2. Clique em **"Execute"**

**Resultado Esperado:** 400 Bad Request com mensagem de erro

### Teste de Moeda Inválida

1. Use:
   ```json
   {
     "amount": 100.00,
     "currency": "USD"
   }
   ```

2. Clique em **"Execute"**

**Resultado Esperado:** 400 Bad Request informando que a moeda deve ser BRL

### Teste de Moeda Vazia

1. Use:
   ```json
   {
     "amount": 100.00,
     "currency": ""
   }
   ```

**Resultado Esperado:** 400 Bad Request informando que a moeda é obrigatória

## Teste 4: Validação de Range

### Valor Muito Alto

1. Use:
   ```json
   {
     "amount": 2000000.00,
     "currency": "BRL"
   }
   ```

**Resultado Esperado:** 400 Bad Request informando o range permitido

### Valor Muito Baixo

1. Use:
   ```json
   {
     "amount": 0.005,
     "currency": "BRL"
   }
   ```

**Resultado Esperado:** 400 Bad Request informando o valor mínimo

## Interpretando as Respostas

### Códigos de Status HTTP

- **200 OK**: Pagamento processado com sucesso
- **400 Bad Request**: Dados de entrada inválidos
- **500 Internal Server Error**: Erro interno do servidor

### Campos da Resposta

- **id**: ID sequencial interno do pagamento
- **externalId**: ID retornado pelo provedor externo (FastPay ou SecurePay)
- **status**: "approved" ou "rejected"
- **provider**: "FastPay" ou "SecurePay"
- **grossAmount**: Valor bruto informado
- **fee**: Taxa calculada conforme o provedor
- **netAmount**: Valor líquido (bruto - taxa)

## Testando o Sistema de Fallback

Para testar o fallback, você precisa configurar os mock servers. Consulte a [documentação dos Mock Servers](MOCK_SERVERS.md) para aprender como:

1. Configurar um mock para falhar
2. Fazer uma requisição que normalmente usaria esse provedor
3. Verificar que o sistema usa o provedor alternativo

## Dicas

- Use o botão **"Clear"** para limpar o campo de requisição
- Use o botão **"Cancel"** para cancelar uma requisição em andamento
- As respostas são exibidas em formato JSON formatado
- Você pode copiar a resposta clicando no ícone de cópia

## Próximos Passos

- Aprenda sobre os [Mock Servers](MOCK_SERVERS.md) para testar cenários avançados
- Consulte a [documentação de testes unitários](DOC_TESTES_UNITARIOS.md) para entender como testar o código
