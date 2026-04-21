# Code Metric Collector

API REST em .NET 8 que coleta métricas de código-fonte a partir de repositórios GitHub. Atualmente suporta a contagem de endpoints HTTP declarados em controllers **Java (Spring Boot)** e **.NET (ASP.NET Core)**.

---

## Endpoints

### `POST /collect`

Recebe a URL de uma pasta de controllers em um repositório GitHub e retorna a quantidade de endpoints HTTP encontrados nos arquivos dessa pasta.

**Request body:**
```json
{
  "urlControllers": "https://github.com/owner/repo/tree/main/src/controllers"
}
```

**Response:**
```json
{
  "metric": {
    "name": "Operations per service",
    "collectorStrategy": "code"
  },
  "measurement": {
    "apiIdentifier": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "value": 5,
    "unit": "operations",
    "timestamp": "2026-04-20T00:00:00.0000000Z"
  }
}
```

---

## Estratégias de detecção

| Linguagem | Arquivo  | Anotações detectadas |
|-----------|----------|----------------------|
| Java (Spring Boot) | `*.java` | `@GetMapping`, `@PostMapping`, `@PutMapping`, `@DeleteMapping`, `@PatchMapping`, `@RequestMapping(method = RequestMethod.*)` |
| .NET (ASP.NET Core) | `*.cs` | `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`, `[HttpHead]`, `[HttpOptions]` |

---

## Executando localmente

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Comandos

```bash
# Restaurar dependências e compilar
dotnet build code-metric-collector.sln

# Executar
dotnet run --project src/CodeMetricCollector.csproj
```

A API ficará disponível em `http://localhost:5000` (ou `https://localhost:5001`).  
O Swagger UI é servido na raiz: `http://localhost:5000`.

---

## Executando com Docker

### Build e execução direta

```bash
docker build -t code-metric-collector .
docker run -p 8080:8080 code-metric-collector
```

### Com Docker Compose

```bash
docker compose up --build
```

A API ficará disponível em `http://localhost:8080`.  
Swagger UI: `http://localhost:8080`.

---

## Configuração

As configurações ficam em `src/appsettings.json`.

| Chave | Descrição | Padrão |
|-------|-----------|--------|
| `GitHub:Token` | Personal Access Token do GitHub (recomendado para evitar rate limit de 60 req/h) | `""` (sem autenticação) |

### Definindo o token

**Via appsettings.json:**
```json
{
  "GitHub": {
    "Token": "ghp_seu_token_aqui"
  }
}
```

**Via variável de ambiente:**
```bash
# Local
export GitHub__Token=ghp_seu_token_aqui

# Docker Compose — descomente no docker-compose.yml:
# - GitHub__Token=ghp_seu_token_aqui
```

---

## Estrutura do projeto

```
code-metric-collector/
├── .dockerignore
├── .gitignore
├── docker-compose.yml
├── Dockerfile
├── code-metric-collector.sln
└── src/
    ├── CodeMetricCollector.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Controllers/
    │   └── CollectController.cs
    ├── Models/
    │   ├── CollectRequest.cs
    │   └── CollectResponse.cs
    └── Services/
        ├── EndpointCountService.cs
        ├── GitHubService.cs
        └── Strategies/
            ├── IEndpointCounterStrategy.cs
            ├── JavaEndpointCounterStrategy.cs
            └── DotNetEndpointCounterStrategy.cs
```

---

## Adicionando suporte a uma nova linguagem

1. Crie uma classe em `src/Services/Strategies/` implementando `IEndpointCounterStrategy`:

```csharp
public class PythonEndpointCounterStrategy : IEndpointCounterStrategy
{
    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".py", StringComparison.OrdinalIgnoreCase);

    public int CountEndpoints(string fileContent)
    {
        // lógica de detecção
    }
}
```

2. Registre no `Program.cs`:

```csharp
builder.Services.AddScoped<IEndpointCounterStrategy, PythonEndpointCounterStrategy>();
```
