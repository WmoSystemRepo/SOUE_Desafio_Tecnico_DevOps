# Guia de Execução com Docker

Este guia fornece instruções detalhadas para executar o sistema PayFlow usando Docker e Docker Compose.

## Pré-requisitos

- Docker Desktop instalado e rodando
- Docker Compose instalado (geralmente incluído com Docker Desktop)
- Portas disponíveis: 5000, 8082, 8083

## Verificação Inicial

Antes de iniciar, verifique se o Docker está funcionando:

```bash
docker --version
docker-compose --version
```

## Execução Rápida

1. Navegue até a pasta do projeto:
   ```bash
   cd SOUE_Desafio_Tecnico_DevOps
   ```

2. Execute o Docker Compose:
   ```bash
   docker-compose up --build
   ```

3. Aguarde até ver as mensagens de inicialização:
   ```
   payflow-api     | Now listening on: http://[::]:8080
   mock-fastpay    | Now listening on: http://[::]:8080
   mock-securepay  | Now listening on: http://[::]:8080
   ```

## Serviços Disponíveis

Após a inicialização, os seguintes serviços estarão disponíveis:

- **PayFlow API**: http://localhost:5000/swagger
- **MockFastPay**: http://localhost:8082/swagger
- **MockSecurePay**: http://localhost:8083/swagger

## Execução em Background

Para executar os containers em background:

```bash
docker-compose up -d --build
```

Para ver os logs:

```bash
docker-compose logs -f
```

## Parar os Serviços

Para parar os containers:

```bash
docker-compose down
```

Para parar e remover volumes:

```bash
docker-compose down -v
```

## Verificar Status

Para verificar quais containers estão rodando:

```bash
docker ps
```

Para ver logs de um container específico:

```bash
docker logs payflow-api
docker logs mock-fastpay
docker logs mock-securepay
```

## Reconstruir Imagens

Se houver alterações no código, reconstrua as imagens:

```bash
docker-compose build --no-cache
docker-compose up
```

## Solução de Problemas

### Porta já em uso

Se receber erro de porta em uso:

1. Identifique qual processo está usando a porta:
   ```bash
   # Windows PowerShell
   netstat -ano | findstr :5000
   ```

2. Pare o processo ou altere a porta no `docker-compose.yml`

### Container não inicia

1. Verifique os logs:
   ```bash
   docker-compose logs payflow-api
   ```

2. Reconstrua as imagens:
   ```bash
   docker-compose down
   docker-compose build --no-cache
   docker-compose up
   ```

### Docker Desktop não está rodando

Certifique-se de que o Docker Desktop está iniciado e aguarde até aparecer "Docker Desktop is running" na bandeja do sistema.

## Próximos Passos

Após subir os containers com sucesso:

1. Acesse o Swagger da API principal: http://localhost:5000/swagger
2. Consulte a [documentação de testes no Swagger](DOC_TESTES_SWAGGER.md) para aprender como testar a API
3. Consulte a [documentação dos Mock Servers](MOCK_SERVERS.md) para entender como controlar os mocks

## Configuração Avançada

### Variáveis de Ambiente

Você pode configurar variáveis de ambiente no `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - PaymentProviders__FastPay__BaseUrl=http://mock-fastpay:8080
  - PaymentProviders__SecurePay__BaseUrl=http://mock-securepay:8080
```

### Rede Docker

Os serviços estão configurados na rede `payflow-network`, permitindo comunicação interna entre containers usando os nomes dos serviços.
