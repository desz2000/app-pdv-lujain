# PDV Lujain

Sistema de Ponto de Venda (PDV) para restaurante por kilo. Composto por:

- **API** ASP.NET Core 8.0 com SQLite (Entity Framework Core)
- **Web** Blazor Server (caixa, cardápio, dashboard, fechar caixa)
- **Mobile** .NET MAUI (operador da balança) — repo [`desz2000/app-mobile-pdv-lujain`](https://github.com/desz2000/app-mobile-pdv-lujain)

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

### Windows (modo fácil)

Duplo-clique em **`start.cmd`** na raiz do repositório. Ele:

1. Abre uma janela rodando a API em `http://0.0.0.0:5170` (aceita conexões da rede)
2. Abre outra janela rodando o caixa em http://localhost:5180
3. Abre o navegador apontando pra http://localhost:5180

Pra desligar, feche as duas janelas (`PDV API` e `PDV Caixa`) ou rode **`stop.cmd`**.

> Na primeira execução, o Windows vai perguntar se quer liberar `dotnet` no Firewall — autorize a caixa **"Redes privadas"** pra que o celular da balança consiga conectar.

### Manual (qualquer SO)

Em dois terminais separados, a partir da raiz do repositório:

```bash
# Terminal 1 — API (escuta em todas as interfaces pra aceitar o celular da balança)
dotnet run --project src/RestaurantePDV.API --urls http://0.0.0.0:5170

# Terminal 2 — Front web
dotnet run --project src/RestaurantePDV.Desktop --urls http://localhost:5180
```

Depois abra http://localhost:5180 no navegador. O PIN padrão é `1234` (configurável em `appsettings.json`).

A API cria o banco SQLite (`pdv-lujain.db`) automaticamente na primeira execução.

### Conectando o celular da balança

A API precisa estar bindada em `0.0.0.0` (o `start.cmd` já faz isso). No app PDV Balança, em **Configurações** → URL da API, use a URL que cabe ao seu cenário:

| Cenário | URL no app |
| ------- | ---------- |
| **Celular real** na mesma Wi-Fi do PC do caixa | `http://<IP-DO-PC>:5170` — descubra o IP com `ipconfig` no Windows (linha "IPv4 Address" da interface Wi-Fi) |
| **Emulador Android** rodando no mesmo PC | `http://10.0.2.2:5170` — `10.0.2.2` é um alias mágico do emulador que aponta pro localhost do host |
| **Genymotion / outro emulador** | depende do emulador; geralmente o IP da LAN do host funciona, ou um alias específico |

> O IP `127.0.0.1` / `localhost` **não funciona** dentro do emulador nem do celular, porque pra eles "localhost" é o próprio dispositivo, não o PC.

> Se o celular ainda não conectar mesmo com `0.0.0.0` e o IP certo, o problema é Firewall do Windows. Vá em "Windows Defender Firewall" → "Permitir um aplicativo" → procure `dotnet` e marque a coluna **Privada**. Ou, mais rápido, libere a porta com este comando em PowerShell como administrador:
>
> ```powershell
> New-NetFirewallRule -DisplayName "PDV API 5170" -Direction Inbound -LocalPort 5170 -Protocol TCP -Action Allow -Profile Private
> ```

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
| POST | `/api/comandas/{numero}/reabrir` | Desfaz o fechamento da comanda mais recente com esse número (se não houver outra aberta no mesmo número) |
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

## Reuso de comandas e reabertura

O número da comanda representa um **cartão físico** que circula pelo salão e pode ser reusado várias vezes ao dia:

- O número só é único entre as comandas **abertas**. Comandas fechadas/canceladas ficam no histórico com o mesmo número.
- Quando o operador da balança lança um item na comanda #5 e não existe nenhuma #5 aberta, o sistema **cria uma comanda nova** automaticamente (mesmo que existam #5 antigas já fechadas). Cada visita de cliente é uma comanda separada nos relatórios.
- Se o caixa fechou uma comanda por engano, ele pode buscar o número e clicar em **"Reabrir esta comanda"** — o sistema volta o status pra Aberta, mantém os itens e zera a forma de pagamento. A API rejeita reabrir se já existir uma comanda aberta com o mesmo número (pra não criar dois "ativos" pro mesmo cartão).

## Roadmap

- App MAUI da balança — feito no repo [`desz2000/app-mobile-pdv-lujain`](https://github.com/desz2000/app-mobile-pdv-lujain)
