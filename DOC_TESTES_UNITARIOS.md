# Guia de Testes Unitários

Este guia explica como executar e criar testes unitários para o sistema PayFlow.

## Visão Geral

Os testes unitários validam o comportamento de componentes individuais do sistema de forma isolada, sem depender de serviços externos ou banco de dados.

## Estrutura de Testes

O projeto de testes unitários está implementado e utiliza xUnit como framework de testes padrão do .NET.

### Estrutura do Projeto

O projeto `PayFlow.Tests` está localizado na raiz da solução e possui a seguinte estrutura:

```
PayFlow.Tests/
├── Services/
│   └── PaymentServiceTests.cs
├── Providers/
│   ├── FastPayProviderTests.cs
│   └── SecurePayProviderTests.cs
└── PayFlow.Tests.csproj
```

### Status dos Testes

O projeto de testes está completamente implementado com 29 testes unitários cobrindo todos os componentes críticos do sistema. Todos os testes estão passando com sucesso.

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

## Componentes Testados

### 1. PaymentService (9 testes)

A classe `PaymentServiceTests` contém testes que validam:

- Seleção correta do provedor primário baseado no valor (< R$100 = FastPay, >= R$100 = SecurePay)
- Cálculo correto de taxas usando o provider selecionado
- Sistema de fallback quando provider primário falha
- Geração sequencial de IDs de pagamento
- Cálculo correto de valores líquidos (gross - fee)
- Status rejected quando ambos providers falham
- Uso correto do provider de fallback quando primário falha
- Valores limite (exatamente R$100.00)

**Testes implementados:**
- `ProcessPayment_AmountLessThanThreshold_ShouldUseFastPay`
- `ProcessPayment_AmountGreaterThanOrEqualThreshold_ShouldUseSecurePay`
- `ProcessPayment_AmountExactlyThreshold_ShouldUseSecurePay`
- `ProcessPayment_FastPayFails_ShouldUseSecurePayAsFallback`
- `ProcessPayment_SecurePayFails_ShouldUseFastPayAsFallback`
- `ProcessPayment_BothProvidersFail_ShouldReturnRejected`
- `ProcessPayment_ShouldGenerateSequentialIds`
- `ProcessPayment_ShouldCalculateNetAmountCorrectly`

### 2. FastPayProvider (9 testes)

A classe `FastPayProviderTests` contém testes que validam:

- Cálculo correto da taxa (3.49% do valor)
- Processamento bem-sucedido com resposta aprovada
- Processamento rejeitado com resposta rejeitada
- Tratamento de erro HTTP (HttpRequestException)
- Tratamento de resposta inválida
- Formatação correta da requisição
- Conversão correta do status da resposta (case-insensitive)

**Testes implementados:**
- `CalculateFee_ShouldReturnCorrectPercentage`
- `ProviderName_ShouldReturnFastPay`
- `ProcessPaymentAsync_SuccessfulResponse_ShouldReturnApproved`
- `ProcessPaymentAsync_RejectedResponse_ShouldReturnRejected`
- `ProcessPaymentAsync_NullResponse_ShouldReturnFailure`
- `ProcessPaymentAsync_HttpRequestException_ShouldReturnFailure`
- `ProcessPaymentAsync_GenericException_ShouldReturnFailure`
- `ProcessPaymentAsync_ShouldFormatRequestCorrectly`
- `ProcessPaymentAsync_StatusCaseInsensitive_ShouldHandleCorrectly`

### 3. SecurePayProvider (11 testes)

A classe `SecurePayProviderTests` contém testes que validam:

- Cálculo correto da taxa (2.99% + R$0.40 fixo)
- Conversão de valor para centavos
- Processamento bem-sucedido com resultado "success"
- Processamento rejeitado com resultado diferente de "success"
- Tratamento de erro HTTP (HttpRequestException)
- Tratamento de resposta inválida
- Formatação correta da requisição
- Geração correta de ClientReference
- Cálculo de taxa com valores zero e grandes

**Testes implementados:**
- `CalculateFee_ShouldReturnCorrectPercentagePlusFixed`
- `ProviderName_ShouldReturnSecurePay`
- `ProcessPaymentAsync_SuccessfulResponse_ShouldReturnApproved`
- `ProcessPaymentAsync_NonSuccessResponse_ShouldReturnRejected`
- `ProcessPaymentAsync_NullResponse_ShouldReturnFailure`
- `ProcessPaymentAsync_HttpRequestException_ShouldReturnFailure`
- `ProcessPaymentAsync_GenericException_ShouldReturnFailure`
- `ProcessPaymentAsync_ShouldConvertAmountToCents`
- `ProcessPaymentAsync_ShouldFormatRequestCorrectly`
- `ProcessPaymentAsync_ShouldGenerateClientReference`
- `CalculateFee_WithZeroAmount_ShouldReturnFixedFee`
- `CalculateFee_WithLargeAmount_ShouldCalculateCorrectly`

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

## Resultados dos Testes

### Execução Completa

Para executar todos os testes unitários:

```bash
dotnet test PayFlow.Tests --verbosity normal
```

**Status atual:** Todos os 29 testes estão passando com sucesso.

### Resumo de Cobertura

- **PaymentService:** 9 testes cobrindo seleção de provider, fallback, cálculos e geração de IDs
- **FastPayProvider:** 9 testes cobrindo cálculo de taxa, processamento, tratamento de erros e formatação
- **SecurePayProvider:** 11 testes cobrindo cálculo de taxa, conversão, processamento e tratamento de erros

### Validação Automatizada

Os testes unitários podem ser executados automaticamente através do script `test-unitarios.ps1` localizado em `C:\Users\User\Desktop\Clones\WMO\Doc\SOUE_Desafio_Tecnico_DevOps\tests\`.

## Próximos Passos

1. ✅ Projeto de testes criado e configurado
2. ✅ Testes implementados para todos os componentes críticos
3. Configure cobertura de código (opcional)
4. Integre testes no pipeline de CI/CD
5. Use os scripts automatizados para validação contínua

## Referências

- [Documentação do xUnit](https://xunit.net/)
- [Documentação do Moq](https://github.com/moq/moq4)
- [Best Practices for Unit Testing](https://docs.microsoft.com/en-us/dotnet/core/testing/)

