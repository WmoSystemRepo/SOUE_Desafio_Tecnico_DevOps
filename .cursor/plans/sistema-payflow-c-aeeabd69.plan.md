<!-- aeeabd69-29cb-4413-af2c-2d2d9b00f761 98f69f6f-cfe0-4358-9f05-e7b13500457d -->
# Implementação de Testes Unitários

## Objetivo

Criar projeto de testes unitários completo para validar toda a lógica de negócio do sistema PayFlow, garantindo código profissional sem comentários e atualizando a documentação após implementação.

## Estrutura do Projeto de Testes

### 1. Criar Projeto PayFlow.Tests

- Criar projeto xUnit na raiz da solução
- Adicionar referências aos projetos: PayFlow.Services, PayFlow.Providers, PayFlow.Domain
- Adicionar pacotes NuGet: xunit, Moq, Microsoft.NET.Test.Sdk

### 2. Estrutura de Pastas

```
PayFlow.Tests/
├── Services/
│   └── PaymentServiceTests.cs
├── Providers/
│   ├── FastPayProviderTests.cs
│   └── SecurePayProviderTests.cs
└── PayFlow.Tests.csproj
```

## Testes a Implementar

### PaymentServiceTests.cs

Testar em `PayFlow.Tests/Services/PaymentServiceTests.cs`:

- Seleção de provider primário baseado em valor (< R$100 = FastPay, >= R$100 = SecurePay)
- Cálculo correto de taxas usando o provider selecionado
- Sistema de fallback quando provider primário falha
- Geração sequencial de IDs de pagamento
- Cálculo correto de valores líquidos (gross - fee)
- Status rejected quando ambos providers falham
- Uso correto do provider de fallback quando primário falha

### FastPayProviderTests.cs

Testar em `PayFlow.Tests/Providers/FastPayProviderTests.cs`:

- Cálculo de taxa (3.49% do valor)
- Processamento bem-sucedido com resposta aprovada
- Processamento rejeitado com resposta rejeitada
- Tratamento de erro HTTP (HttpRequestException)
- Tratamento de resposta vazia (null)
- Formatação correta da requisição
- Conversão correta do status da resposta

### SecurePayProviderTests.cs

Testar em `PayFlow.Tests/Providers/SecurePayProviderTests.cs`:

- Cálculo de taxa (2.99% + R$0.40 fixo)
- Conversão de valor para centavos
- Processamento bem-sucedido com resultado "success"
- Processamento rejeitado com resultado diferente de "success"
- Tratamento de erro HTTP (HttpRequestException)
- Tratamento de resposta vazia (null)
- Formatação correta da requisição
- Geração correta de ClientReference

## Mocking e Dependências

### Para PaymentService

- Usar Moq para criar mocks de IPaymentProvider
- Configurar mocks para retornar PaymentResult com diferentes cenários
- Validar que métodos CalculateFee são chamados corretamente

### Para Providers

- Usar Mock<HttpMessageHandler> para simular respostas HTTP
- Criar HttpClient com handler mockado
- Testar diferentes cenários de resposta (sucesso, erro, timeout)

## Validações e Assertions

### Usar Assert padrão do xUnit

- Assert.Equal para valores exatos
- Assert.True/False para condições booleanas
- Assert.Null/NotNull para valores nulos
- Assert.Contains para strings

### Valores de Teste

- Usar PaymentConstants para valores de threshold, taxas e status
- Testar valores limite (99.99, 100.00, 100.01)
- Testar valores extremos quando aplicável

## Código Profissional

### Nomenclatura

- Nomes de testes descritivos seguindo padrão: `MethodName_Scenario_ExpectedResult`
- Exemplo: `ProcessPayment_AmountLessThan100_ShouldUseFastPay`

### Organização

- Uma classe de teste por classe testada
- Métodos de teste organizados por funcionalidade
- Setup de mocks em métodos auxiliares quando necessário

### Sem Comentários

- Código autoexplicativo
- Nenhum comentário no código de teste
- Nomes de variáveis descritivos

## Verificação de Código Existente

### Revisar código atual

- Verificar se há padrões suspeitos de IA (comentários excessivos, nomes genéricos)
- Garantir que código de produção está profissional
- Remover qualquer vestígio de comentários se encontrado

## Execução e Validação

### Após implementação

- Executar `dotnet test` para validar todos os testes
- Garantir que todos os testes passam
- Executar script `test-unitarios.ps1` para validar integração
- Verificar cobertura de código (opcional)

## Atualização de Documentação

### Após criar e testar os testes unitários

- Atualizar `DOC_TESTES_UNITARIOS.md`:
  - Adicionar seção confirmando que testes foram implementados
  - Atualizar exemplos se necessário
  - Adicionar informações sobre como executar os testes criados
  - Incluir informações sobre cobertura de testes
  - Adicionar seção sobre estrutura de testes criada
- Verificar se `README.md` precisa de atualizações relacionadas aos testes
- Garantir que documentação está consistente e atualizada

## Arquivos a Criar/Modificar

1. `PayFlow.Tests/PayFlow.Tests.csproj` - Novo arquivo
2. `PayFlow.Tests/Services/PaymentServiceTests.cs` - Novo arquivo
3. `PayFlow.Tests/Providers/FastPayProviderTests.cs` - Novo arquivo
4. `PayFlow.Tests/Providers/SecurePayProviderTests.cs` - Novo arquivo
5. Atualizar `PayFlow.sln` para incluir projeto de testes
6. Atualizar `DOC_TESTES_UNITARIOS.md` com informações dos testes implementados