# PDV Lujain

Sistema de Ponto de Venda (PDV) para restaurante por kilo. Composto por:

- **API** ASP.NET Core 8.0 com SQLite (Entity Framework Core)
- **Web** Blazor Server (caixa, cardápio, dashboard, fechar caixa)
- **Mobile** .NET MAUI (operador da balança) — _em PR separado_

## Estrutura

```
src/
├── RestaurantePDV.Core/          entidades de domínio e enums
├── RestaurantePDV.Contracts/     DTOs compartilhados entre API e clientes
├── RestaurantePDV.Data/          DbContext e configuração EF Core
├── RestaurantePDV.API/           Web API, controllers, serviços (Excel, relatório)
└── RestaurantePDV.Desktop/       Blazor Server (front do caixa)

tests/
└── RestaurantePDV.Tests/         xUnit (unitários + integração API)
```

## Pré-requisitos

- .NET SDK 8.0

## Como rodar

Em dois terminais separados, a partir da raiz do repositório:

```bash
# Terminal 1 — API
dotnet run --project src/RestaurantePDV.API --urls http://localhost:5170

# Terminal 2 — Front web
dotnet run --project src/RestaurantePDV.Desktop --urls http://localhost:5180
```

Depois abra http://localhost:5180 no navegador. O PIN padrão é `1234` (configurável em `appsettings.json`).

A API cria o banco SQLite (`pdv-lujain.db`) automaticamente na primeira execução.

## Configuração

`src/RestaurantePDV.API/appsettings.json`:

```json
{
  "ConnectionStrings": { "Default": "Data Source=pdv-lujain.db" },
  "App": { "Pin": "1234", "RequirePinHeader": false },
  "Cors": { "AllowedOrigins": ["http://localhost:5180"] }
}
```

`src/RestaurantePDV.Desktop/appsettings.json`:

```json
{ "Api": { "BaseUrl": "http://localhost:5170" } }
```

## Endpoints principais da API

| Método | Rota | Função |
| ------ | ---- | ------ |
| POST | `/api/auth/validar-pin` | Valida PIN |
| GET | `/api/produtos` | Lista produtos (`?incluirInativos=true`) |
| POST | `/api/produtos` | Cria produto |
| PUT | `/api/produtos/{id}` | Atualiza produto |
| DELETE | `/api/produtos/{id}` | Inativa produto |
| GET | `/api/comandas/{numero}` | Busca comanda |
| POST | `/api/comandas/{numero}/itens` | Adiciona item (cria comanda se não existir) |
| DELETE | `/api/comandas/{numero}/itens/{itemId}` | Remove item |
| POST | `/api/comandas/{numero}/fechar` | Fecha com forma de pagamento |
| POST | `/api/comandas/{numero}/cancelar` | Cancela comanda |
| GET | `/api/relatorios/dia?data=YYYY-MM-DD` | Relatório do dia em JSON |
| GET | `/api/relatorios/dia/excel?data=YYYY-MM-DD` | Excel com 4 abas (Resumo, Forma de Pagamento, Por Hora, Top Produtos) |

Swagger disponível em http://localhost:5170/swagger durante o desenvolvimento.

## Tests

```bash
dotnet test
```

Os testes de integração usam SQLite in-memory (mesma engine de produção) e cobrem fluxos de comanda, CRUD de produtos, validação de PIN e geração de Excel.

## Formas de pagamento

- Dinheiro
- Débito
- Crédito
- Pix
- Vale Refeição
- Vale Alimentação

## Roadmap

- App MAUI da balança (PIN + nº comanda + valor + adicionar) — próximo PR
