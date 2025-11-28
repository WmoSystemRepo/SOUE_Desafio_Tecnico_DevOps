# Guia de Testes Unitários

Este guia explica como executar e criar testes unitários para o sistema PayFlow.

## Visão Geral

Os testes unitários validam o comportamento de componentes individuais do sistema de forma isolada, sem depender de serviços externos ou banco de dados.

## Estrutura de Testes

O projeto está preparado para testes unitários usando xUnit, o framework de testes padrão do .NET.

### Criar Projeto de Testes

Para criar um projeto de testes, execute:

```bash
dotnet new xunit -n PayFlow.Tests
cd PayFlow.Tests
dotnet add reference ../PayFlow.Services/PayFlow.Services.csproj
dotnet add reference ../PayFlow.Providers/PayFlow.Providers.csproj
dotnet add reference ../PayFlow.Domain/PayFlow.Domain.csproj
```

## Executar Testes

### Executar Todos os Testes

```bash
dotnet test
```

### Executar Testes de um Projeto Específico

```bash
dotnet test PayFlow.Tests
```

### Executar com Cobertura de Código

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Componentes a Testar

### 1. PaymentService

Testes para validar:
- Seleção correta do provedor baseado no valor
- Cálculo correto de taxas
- Funcionamento do sistema de fallback
- Geração de IDs sequenciais
- Cálculo de valores líquidos

**Exemplo de teste:**
```csharp
[Fact]
public async Task ProcessPayment_AmountLessThan100_ShouldUseFastPay()
{
    // Arrange
    var fastPayProvider = new Mock<IPaymentProvider>();
    var securePayProvider = new Mock<IPaymentProvider>();
    var service = new PaymentService(fastPayProvider.Object, securePayProvider.Object);
    
    // Act
    var result = await service.ProcessPaymentAsync(new PaymentRequest 
    { 
        Amount = 50.00m, 
        Currency = "BRL" 
    });
    
    // Assert
    Assert.Equal("FastPay", result.Provider);
}
```

### 2. FastPayProvider

Testes para validar:
- Cálculo correto da taxa (3.49%)
- Formatação correta da requisição
- Parsing correto da resposta
- Tratamento de erros HTTP
- Conversão de status

### 3. SecurePayProvider

Testes para validar:
- Cálculo correto da taxa (2.99% + R$ 0.40)
- Conversão de valor para centavos
- Formatação correta da requisição
- Parsing correto da resposta
- Tratamento de erros HTTP

### 4. PaymentConstants

Testes para validar:
- Valores das constantes estão corretos
- Constantes são imutáveis

## Mocking de Dependências

### Mock de HttpClient

Para testar providers sem fazer chamadas HTTP reais:

```csharp
var mockHttp = new Mock<HttpMessageHandler>();
var httpClient = new HttpClient(mockHttp.Object);
var provider = new FastPayProvider(httpClient, "http://test.com");
```

### Mock de IPaymentProvider

Para testar PaymentService:

```csharp
var mockProvider = new Mock<IPaymentProvider>();
mockProvider.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
    .ReturnsAsync(new PaymentResult { IsSuccess = true, Status = "approved" });
```

## Testes de Integração

Testes de integração validam a interação entre múltiplos componentes:

### Teste de Endpoint Completo

```csharp
[Fact]
public async Task PostPayments_ValidRequest_ReturnsOk()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new { amount = 100.00m, currency = "BRL" };
    
    // Act
    var response = await client.PostAsJsonAsync("/payments", request);
    
    // Assert
    response.EnsureSuccessStatusCode();
}
```

## Boas Práticas

### 1. Nomenclatura

Use nomes descritivos que expliquem o que está sendo testado:

```csharp
[Fact]
public async Task ProcessPayment_AmountGreaterThan100_ShouldUseSecurePay()
```

### 2. Arrange-Act-Assert

Sempre siga o padrão AAA:
- **Arrange**: Configure o estado inicial
- **Act**: Execute a ação a ser testada
- **Assert**: Verifique o resultado

### 3. Testes Isolados

Cada teste deve ser independente e não depender de outros testes.

### 4. Cobertura

Busque alta cobertura de código, especialmente para:
- Lógica de negócio
- Cálculos
- Validações
- Tratamento de erros

## Executar Testes no CI/CD

### GitHub Actions

```yaml
- name: Run tests
  run: dotnet test --verbosity normal
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    projects: '**/*Tests/*.csproj'
```

## Ferramentas Úteis

### xUnit

Framework de testes padrão do .NET.

### Moq

Biblioteca para criar mocks:

```bash
dotnet add package Moq
```

### FluentAssertions

Biblioteca para assertions mais legíveis:

```bash
dotnet add package FluentAssertions
```

### Coverlet

Para cobertura de código:

```bash
dotnet add package coverlet.msbuild
```

## Exemplo Completo

```csharp
using Xunit;
using Moq;
using PayFlow.Services;
using PayFlow.Domain.Interfaces;
using PayFlow.Domain.Models;
using PayFlow.Domain.Constants;

namespace PayFlow.Tests.Services;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPayment_AmountLessThanThreshold_ShouldUseFastPay()
    {
        var fastPayMock = new Mock<IPaymentProvider>();
        fastPayMock.Setup(p => p.ProviderName).Returns(PaymentConstants.ProviderFastPay);
        fastPayMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new PaymentResult 
            { 
                IsSuccess = true, 
                Status = PaymentConstants.StatusApproved,
                ExternalId = "FP-123",
                ProviderName = PaymentConstants.ProviderFastPay
            });
        fastPayMock.Setup(p => p.CalculateFee(It.IsAny<decimal>())).Returns(1.745m);

        var securePayMock = new Mock<IPaymentProvider>();
        var service = new PaymentService(fastPayMock.Object, securePayMock.Object);

        var request = new PaymentRequest { Amount = 50.00m, Currency = "BRL" };
        var result = await service.ProcessPaymentAsync(request);

        Assert.Equal(PaymentConstants.ProviderFastPay, result.Provider);
        Assert.Equal(PaymentConstants.StatusApproved, result.Status);
        Assert.Equal(1.745m, result.Fee);
        Assert.Equal(48.255m, result.NetAmount);
    }
}
```

## Próximos Passos

1. Crie o projeto de testes conforme instruções acima
2. Implemente testes para cada componente crítico
3. Configure cobertura de código
4. Integre testes no pipeline de CI/CD
5. Use os scripts automatizados para validação contínua

## Referências

- [Documentação do xUnit](https://xunit.net/)
- [Documentação do Moq](https://github.com/moq/moq4)
- [Best Practices for Unit Testing](https://docs.microsoft.com/en-us/dotnet/core/testing/)

