# EasierPGW - Pay&Go Web Library Wrapper

Uma implementação simplificada e orientada a eventos para facilitar o uso da biblioteca Pay&Go Web em aplicações .NET, abstraindo a complexidade da comunicação direta com a DLL nativa do Windows.

## Sobre

EasierPGW é um wrapper C# que simplifica drasticamente o uso da biblioteca Pay&Go Web (PGWebLib.dll), transformando o complexo fluxo de funções nativas em uma interface orientada a eventos, intuitiva e fácil de usar.

### Problemas Solucionados

A biblioteca Pay&Go Web original requer:
- Múltiplas chamadas de funções nativas em sequência específica
- Gerenciamento manual de estados e loops de eventos
- Tratamento complexo de estruturas C/C++ não gerenciadas
- Controle manual de pendências e confirmações de transação
- Implementação específica para cada tipo de captura de dados

## Características Principais

### Arquitetura Orientada a Eventos
- **Interface Simples**: Um único ponto de entrada (`PGW` class) com eventos para interação
- **Delegates Tipados**: Eventos específicos para cada tipo de interação do usuário
- **Fluxo Automatizado**: Gerenciamento automático do ciclo de vida das transações

### Abstração Completa da DLL
- **Interop Gerenciado**: Todas as chamadas P/Invoke encapsuladas na classe `Interop`
- **Marshalling Automático**: Conversão automática entre tipos gerenciados e não gerenciados
- **Estruturas Tipadas**: Classes C# para todas as estruturas da biblioteca nativa

### Gerenciamento Inteligente de Transações
- **Estados Automatizados**: Controle automático de pendências e confirmações
- **Recovery Resiliente**: Tratamento automático de falhas e recuperação de transações
- **Logging Integrado**: Sistema de log detalhado para debugging e auditoria

## Arquitetura

### Camadas da Aplicação

```
┌─────────────────────────────────────────┐
│              Aplicação                  │ ← Sua aplicação
├─────────────────────────────────────────┤
│           EasierPGW (PGW)               │ ← Wrapper principal
├─────────────────────────────────────────┤
│      Interactions & PINPadInteractions  │ ← Gerenciamento de interações
├─────────────────────────────────────────┤
│            Interop Layer                │ ← Chamadas P/Invoke
├─────────────────────────────────────────┤
│         PGWebLib.dll (Nativa)           │ ← Biblioteca Pay&Go Web
└─────────────────────────────────────────┘
```

### Componentes Principais

#### 1. **PGW.cs** - Classe Principal
- Ponto de entrada único para todas as operações
- Gerencia o ciclo de vida completo das transações
- Implementa o padrão de resolução de pendências

#### 2. **Interactions.cs** - Gerenciador de Interações
- Mapeia tipos de dados para ações específicas
- Gerencia prompts, menus e entrada de dados
- Traduz entre eventos da aplicação e biblioteca nativa

#### 3. **PINPadInteractions.cs** - Interações do PIN-pad
- Especializado em operações específicas do PIN-pad
- Gerencia leitura de cartão, PIN, e comandos genéricos
- Controla o loop de eventos do dispositivo

#### 4. **Interop.cs** - Camada de Interoperabilidade
- Declarações P/Invoke para todas as funções da DLL
- Documentação completa de cada função nativa
- Marshalling correto de parâmetros e estruturas

#### 5. **CustomObjects.cs** - Estruturas Gerenciadas
- Equivalentes C# das estruturas nativas
- Marshalling automático com LayoutKind.Sequential
- Tipos seguros para operações e parâmetros

## Como Usar

### Inicialização
```csharp
var pgw = new PGW(
    onMessageRaising: (message, timeout) => {
        // Exibir mensagem para o usuário
        Console.WriteLine(message);
    },
    onPromptConfirmationRaising: async (message, timeout) => {
        // Solicitar confirmação do usuário
        Console.WriteLine($"Confirmar: {message} (S/N)");
        var response = Console.ReadLine();
        return response?.ToUpper() == "S" ? 
            PromptConfirmationResult.Confirm : 
            PromptConfirmationResult.Cancel;
    },
    onPromptInputRaising: async (config) => {
        // Capturar entrada do usuário
        Console.WriteLine(config.Message);
        return Console.ReadLine();
    },
    onPromptMenuRaising: async (options, defaultOption) => {
        // Exibir menu e capturar seleção
        for (int i = 0; i < options.Count; i++) {
            Console.WriteLine($"{i + 1}. {options[i]}");
        }
        var input = Console.ReadLine();
        return int.TryParse(input, out int choice) && choice > 0 && choice <= options.Count
            ? options[choice - 1] : defaultOption;
    }
);
```

### Executando Operações
```csharp
// Instalação
var installResult = pgw.Installation();

// Operação de venda
var saleResult = pgw.Operation(E_PWOPER.PWOPER_SALE);

// Listar operações disponíveis
var operations = pgw.GetOperations();
```

## Mapeamento de Funcionalidades

### Função Original → EasierPGW

| Biblioteca Original | EasierPGW | Descrição |
|---------------------|-----------|-----------|
| `PW_iInit` + `PW_iNewTransac` + loop manual | `pgw.Operation()` | Execução completa de transação |
| `PW_iGetOperations` | `pgw.GetOperations()` | Lista operações disponíveis |
| `PW_iAddParam` + validação manual | Eventos de `PromptInput` | Captura automática de parâmetros |
| `PW_iPPEventLoop` + estados manuais | `LoopPP()` interno | Loop automático de eventos |
| `PW_iConfirmation` + persistência manual | Automático | Gerenciamento de pendências |

### Tipos de Dados Suportados

| Tipo (E_PWDAT) | Classe Responsável | Funcionalidade |
|----------------|-------------------|----------------|
| `PWDAT_TYPED` | `Interactions` | Entrada digitada pelo usuário |
| `PWDAT_MENU` | `Interactions` | Seleção de menu |
| `PWDAT_CARDINF` | `PINPadInteractions` | Leitura de cartão |
| `PWDAT_PPENCPIN` | `PINPadInteractions` | Captura de PIN |
| `PWDAT_DSPCHECKOUT` | `Interactions` | Exibição de mensagens |
| `PWDAT_DSPQRCODE` | `Interactions` | QR Code (implementação pendente) |

## Fluxo de Transação Simplificado

### Biblioteca Original (Complexa)
```csharp
// 1. Inicialização
PW_iInit(workingDir);

// 2. Nova transação
PW_iNewTransac(operation);

// 3. Adicionar parâmetros obrigatórios
PW_iAddParam(PWINFO_AUTNAME, "App");
PW_iAddParam(PWINFO_AUTVER, "1.0");
// ... mais parâmetros

// 4. Loop de execução
while (true) {
    var result = PW_iExecTransac(structParam, ref numDados);
    switch (result) {
        case PWRET_MOREDATA:
            // Capturar dados adicionais
            // Implementar lógica específica para cada tipo
            break;
        case PWRET_OK:
            // Processar resultado
            break;
        // ... outros casos
    }
}

// 5. Confirmação manual
if (needsConfirmation) {
    PW_iConfirmation(status, reqNum, locRef, extRef, virtMerch, authSyst);
}
```

### EasierPGW (Simplificado)
```csharp
// Tudo em uma linha!
var result = pgw.Operation(E_PWOPER.PWOPER_SALE);
// Os eventos são disparados automaticamente conforme necessário
```

## Estrutura do Projeto

```
EasierPGW/
├── PGW.cs                      # Classe principal
├── Interop.cs                  # Declarações P/Invoke
├── CustomObjects.cs            # Estruturas gerenciadas
├── Enums.cs                    # Enumerações da biblioteca
├── Interactions.cs             # Gerenciador de interações
├── PINPadInteractions.cs       # Interações específicas do PIN-pad
├── Delegates.cs                # Definições de delegates
├── Logger.cs                   # Sistema de logging
├── AppSettings.cs              # Configurações
├── DeviceManagement.cs         # Gerenciamento de dispositivos
├── PromptConfirmationResult.cs # Tipos de resultado
└── PGWebLib.dll               # Biblioteca nativa
```

## Pré-requisitos

- .NET Framework 4.7.2+ ou .NET Core 3.1+
- Windows (biblioteca nativa requer Windows)
- PGWebLib.dll (incluída no projeto)
- Dispositivo PIN-pad compatível (para operações com cartão)

## Execução

### Biblioteca Principal

Para usar a biblioteca EasierPGW em seu projeto:

```bash
dotnet add reference EasierPGW
```

### Projeto de Testes

Para executar os testes que demonstram a funcionalidade:

```bash
dotnet run --project EasierPGW.Tests
```

## Testes

O projeto inclui um conjunto abrangente de testes que demonstram:

### 1. **Testes de Event Handlers**
- Validação dos manipuladores de eventos do README
- Simulação de cenários de interação do usuário
- Demonstração de diferentes tipos de entrada (numérica, texto, senha, menu)

### 2. **Integração com Mock**
- Demonstração da funcionalidade de mock da DLL
- Simulação do fluxo completo de transações
- Teste de operações sem necessidade do hardware real

### 3. **Validação de Configuração**
- Teste de objetos de configuração
- Validação de enums e constantes
- Verificação da integridade dos tipos

### Executando os Testes

```bash
# Navegar para o diretório do projeto
cd easier-pgw

# Executar todos os testes
dotnet run --project EasierPGW.Tests

# Construir o projeto (verificar se compila)
dotnet build
```

### Saída Esperada dos Testes

```
=== EasierPGW Tests ===
Testing Pay&Go Web library wrapper event handlers with simulated scenarios

Test 1: Event Handlers
Testing Message Handler:
MESSAGE: Transação processada com sucesso
Testing Confirmation Handler:
CONFIRMATION: Confirma a operação de venda?
Testing Input Handler:
INPUT REQUEST: Digite o valor da transação
Testing Menu Handler:
MENU SELECTION: [Cartão de Crédito, Cartão de Débito, Voucher]

Test 2: Mock Integration Demonstration
Mock Interop Operations:
[MOCK] PW_iInit: /tmp/test - Result: PWRET_OK
[MOCK] PW_iNewTransac: PWOPER_SALE - Result: PWRET_OK

Test 3: Configuration Validation
Config: PWINFO_TOTAMNT - Valor da transação (Numeric)
Operation: PWOPER_SALE (33)

All tests completed successfully!
```

### Estrutura dos Testes

```
EasierPGW.Tests/
├── Program.cs              # Programa principal de testes
├── MockInterop.cs          # Implementação mock da DLL
├── IPGWInterop.cs         # Interface para abstração de DLL
└── EasierPGW.Tests.csproj # Configuração do projeto
```

### Benefícios dos Testes

- **Desenvolvimento Offline**: Teste a lógica sem hardware PIN-pad
- **Integração Contínua**: Execute testes automatizados em CI/CD
- **Demonstração**: Veja como implementar os event handlers
- **Validação**: Confirme que sua integração está correta

## Exemplos de Uso

### Exemplo Básico - Venda
```csharp
class Program {
    static void Main(string[] args) {
        var pgw = new PGW(
            onMessageRaising: ShowMessage,
            onPromptConfirmationRaising: ConfirmAction,
            onPromptInputRaising: GetUserInput,
            onPromptMenuRaising: SelectFromMenu
        );

        // Executar uma venda
        var result = pgw.Operation(E_PWOPER.PWOPER_SALE);
        
        Console.WriteLine($"Resultado: {result}");
    }

    static void ShowMessage(string message, int? timeout) {
        Console.WriteLine($"MESSAGE: {message}");
        if (timeout.HasValue) {
            Thread.Sleep(timeout.Value);
        }
    }

    static async Task<PromptConfirmationResult> ConfirmAction(string message, int? timeout) {
        Console.WriteLine($"CONFIRMATION: {message} (S/N)");
        var key = Console.ReadKey();
        return key.Key == ConsoleKey.S ? 
            PromptConfirmationResult.Confirm : 
            PromptConfirmationResult.Cancel;
    }

    static async Task<string> GetUserInput(PromptConfig config) {
        Console.WriteLine($"INPUT: {config.Message}");
        if (config.InputType == PromptFieldType.Password) {
            return ReadPassword();
        }
        return Console.ReadLine();
    }

    static async Task<string> SelectFromMenu(List<string> options, string defaultOption) {
        Console.WriteLine("MENU: Selecione uma opção:");
        for (int i = 0; i < options.Count; i++) {
            Console.WriteLine($"  {i + 1}. {options[i]}");
        }
        
        var input = Console.ReadLine();
        if (int.TryParse(input, out int choice) && choice > 0 && choice <= options.Count) {
            return options[choice - 1];
        }
        return defaultOption;
    }
}
```

## Debugging e Logs

O EasierPGW inclui um sistema de logging detalhado que registra:
- Todas as chamadas para a biblioteca nativa
- Parâmetros enviados e recebidos
- Estados das transações
- Erros e exceções

```csharp
// Os logs são automaticamente gerados em:
// %CommonApplicationData%\PGWebLib\
```

## Tratamento de Erros

### Códigos de Retorno
O wrapper preserva todos os códigos de retorno da biblioteca original (enum `E_PWRET`), mas adiciona tratamento automático para:
- Resolução de pendências
- Recovery de transações interrompidas
- Validação de parâmetros
- Timeouts e cancelamentos

### Recuperação Automatizada
```csharp
// A biblioteca automaticamente:
// 1. Detecta transações pendentes na inicialização
// 2. Tenta resolver pendências automaticamente
// 3. Implementa retry logic para falhas de comunicação
// 4. Gerencia estados de transação de forma consistente
```

## Estratégia de Testes

### Abordagem Híbrida

O EasierPGW oferece duas abordagens para testes:

#### 1. **Testes com Mock (EasierPGW.Tests)**
- **Sem dependências**: Não requer DLL ou hardware
- **Rápidos**: Execução instantânea para CI/CD
- **Determinísticos**: Resultados previsíveis
- **Cobertura completa**: Testa todos os cenários de erro

```bash
# Execução rápida para desenvolvimento
dotnet run --project EasierPGW.Tests
```

#### 2. **Testes de Integração (Hardware Real)**
- **Validação real**: Testa com hardware e DLL verdadeiros
- **Cenários reais**: Comportamento idêntico ao ambiente de produção
- **Requer hardware**: PIN-pad e configuração adequada

```csharp
// Para testes de integração, use a classe PGW diretamente
var pgw = new PGW(/* event handlers */);
var result = pgw.Operation(E_PWOPER.PWOPER_SALE);
```

### Recomendações

**Durante o Desenvolvimento:**
1. Use `EasierPGW.Tests` para desenvolvimento rápido
2. Valide a lógica de event handlers
3. Teste cenários de erro e edge cases

**Antes da Produção:**
1. Execute testes de integração com hardware real
2. Valide operações críticas (venda, cancelamento)
3. Teste com diferentes tipos de cartão

**Em CI/CD:**
1. Execute apenas os testes mock para velocidade
2. Configure testes de integração em ambiente dedicado
3. Use testes mock para validação de pull requests

## Contribuindo

Para contribuir com o projeto:

1. Faça um fork do repositório
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## Agradecimentos

- Equipe Pay&Go Web pela biblioteca original
- Comunidade .NET pela documentação sobre P/Invoke
- Contribuidores que ajudaram a testar e melhorar a implementação

## Suporte

Para dúvidas sobre a implementação EasierPGW:
- Abra uma [Issue](https://github.com/seu-usuario/easier-pgw/issues)
- Consulte a [documentação oficial do Pay&Go Web](https://paygodev.readme.io/docs/6-fun%C3%A7%C3%B5es-da-biblioteca)

Para questões sobre a biblioteca Pay&Go Web original:
- Consulte a documentação oficial
- Entre em contato com o suporte Pay&Go Web