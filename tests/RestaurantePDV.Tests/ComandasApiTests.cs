using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;
using RestaurantePDV.Data;

namespace RestaurantePDV.Tests;

public class ComandasApiTests
{
    private static PdvApiFactory NewFactory() => new();

    [Fact]
    public async Task FluxoCompletoComanda_AbrirAdicionarFechar()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var numero = 1001;

        var resp = await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest
        {
            Descricao = "Prato por kilo",
            Valor = 35.50m,
            Origem = OrigemItem.Balanca
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var comanda = await resp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.NotNull(comanda);
        Assert.Equal(StatusComanda.Aberta, comanda!.Status);
        Assert.Single(comanda.Itens);
        Assert.Equal(35.50m, comanda.ValorTotal);

        resp = await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest
        {
            Descricao = "Suco",
            Valor = 7.50m,
            Origem = OrigemItem.Caixa
        });
        comanda = await resp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(43m, comanda!.ValorTotal);
        Assert.Equal(2, comanda.Itens.Count);

        resp = await client.PostAsJsonAsync($"/api/comandas/{numero}/fechar", new FecharComandaRequest
        {
            FormaPagamento = FormaPagamento.Pix
        });
        comanda = await resp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(StatusComanda.Fechada, comanda!.Status);
        Assert.Equal(FormaPagamento.Pix, comanda.FormaPagamento);
        Assert.NotNull(comanda.FechadaEm);

        // Comanda fechada e o cartao foi devolvido: novo cliente usa o mesmo numero,
        // operador da balanca lanca de novo -> sistema cria uma comanda nova com o mesmo numero.
        resp = await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest
        {
            Descricao = "Novo cliente",
            Valor = 1m,
            Origem = OrigemItem.Balanca
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var novaComanda = await resp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.NotNull(novaComanda);
        Assert.Equal(StatusComanda.Aberta, novaComanda!.Status);
        Assert.Equal(numero, novaComanda.Numero);
        Assert.NotEqual(comanda.Id, novaComanda.Id);
        Assert.Single(novaComanda.Itens);
        Assert.Equal(1m, novaComanda.ValorTotal);
    }

    [Fact]
    public async Task ReusoDeNumero_AposFechar_CriaComandaNova()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var numero = 5;

        await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "Almoco", Valor = 30m });
        var fecharResp = await client.PostAsJsonAsync($"/api/comandas/{numero}/fechar", new FecharComandaRequest { FormaPagamento = FormaPagamento.Dinheiro });
        var fechada = await fecharResp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(StatusComanda.Fechada, fechada!.Status);

        // Cliente novo usa o mesmo cartao #5.
        var novaResp = await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "Jantar", Valor = 45m });
        Assert.Equal(HttpStatusCode.OK, novaResp.StatusCode);
        var nova = await novaResp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.NotEqual(fechada.Id, nova!.Id);
        Assert.Equal(StatusComanda.Aberta, nova.Status);
        Assert.Equal(45m, nova.ValorTotal);

        // GET retorna a aberta, nao a fechada.
        var getResp = await client.GetAsync($"/api/comandas/{numero}");
        var get = await getResp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(nova.Id, get!.Id);
        Assert.Equal(StatusComanda.Aberta, get.Status);
    }

    [Fact]
    public async Task Reabrir_ComandaFechada_VoltaParaAberta_ComItens()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var numero = 7;

        await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "Prato", Valor = 25m, Origem = OrigemItem.Balanca });
        await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "Refri", Valor = 6m });
        var fecharResp = await client.PostAsJsonAsync($"/api/comandas/{numero}/fechar", new FecharComandaRequest { FormaPagamento = FormaPagamento.Pix });
        var fechada = await fecharResp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(StatusComanda.Fechada, fechada!.Status);
        Assert.Equal(31m, fechada.ValorTotal);

        var reabrirResp = await client.PostAsync($"/api/comandas/{numero}/reabrir", content: null);
        Assert.Equal(HttpStatusCode.OK, reabrirResp.StatusCode);
        var reaberta = await reabrirResp.Content.ReadFromJsonAsync<ComandaDto>();
        Assert.Equal(fechada.Id, reaberta!.Id);
        Assert.Equal(StatusComanda.Aberta, reaberta.Status);
        Assert.Null(reaberta.FormaPagamento);
        Assert.Null(reaberta.FechadaEm);
        Assert.Equal(2, reaberta.Itens.Count);
        Assert.Equal(31m, reaberta.ValorTotal);
    }

    [Fact]
    public async Task Reabrir_ComandaInexistente_RetornaNotFound()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsync("/api/comandas/77777/reabrir", content: null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Reabrir_ComOutraComandaAbertaNoMesmoNumero_Falha()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var numero = 9;

        // Fecha a primeira comanda 9.
        await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "v1", Valor = 10m });
        await client.PostAsJsonAsync($"/api/comandas/{numero}/fechar", new FecharComandaRequest { FormaPagamento = FormaPagamento.Dinheiro });

        // Cria uma nova com o mesmo numero (novo cliente).
        await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest { Descricao = "v2", Valor = 15m });

        // Tentar reabrir a antiga agora tem que falhar (ja existe uma aberta).
        var reabrirResp = await client.PostAsync($"/api/comandas/{numero}/reabrir", content: null);
        Assert.Equal(HttpStatusCode.BadRequest, reabrirResp.StatusCode);
    }

    [Fact]
    public async Task ComandaSemItens_NaoPodeFechar()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/comandas/9999/fechar", new FecharComandaRequest
        {
            FormaPagamento = FormaPagamento.Dinheiro
        });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task BuscarComandaInexistente_RetornaNotFound()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/comandas/123456");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task CrudProdutos()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/produtos", new CriarProdutoRequest
        {
            Nome = "Suco Natural",
            Preco = 8.50m,
            Tipo = TipoProduto.PrecoFixo
        });
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var criado = await resp.Content.ReadFromJsonAsync<ProdutoDto>();
        Assert.NotNull(criado);
        Assert.True(criado!.Ativo);

        var lista = await client.GetFromJsonAsync<List<ProdutoDto>>("/api/produtos");
        Assert.NotNull(lista);
        Assert.Contains(lista!, p => p.Id == criado.Id);

        resp = await client.PutAsJsonAsync($"/api/produtos/{criado.Id}", new AtualizarProdutoRequest
        {
            Nome = "Suco Natural Grande",
            Preco = 10m,
            Tipo = TipoProduto.PrecoFixo,
            Ativo = true
        });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var atualizado = await resp.Content.ReadFromJsonAsync<ProdutoDto>();
        Assert.Equal("Suco Natural Grande", atualizado!.Nome);

        resp = await client.DeleteAsync($"/api/produtos/{criado.Id}");
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var listaAtiva = await client.GetFromJsonAsync<List<ProdutoDto>>("/api/produtos");
        Assert.DoesNotContain(listaAtiva!, p => p.Id == criado.Id);

        var listaTodos = await client.GetFromJsonAsync<List<ProdutoDto>>("/api/produtos?incluirInativos=true");
        Assert.Contains(listaTodos!, p => p.Id == criado.Id && !p.Ativo);
    }

    [Fact]
    public async Task ValidarPin_OkQuandoCorreto()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/validar-pin", new ValidarPinRequest { Pin = "1234" });
        var dto = await resp.Content.ReadFromJsonAsync<ValidarPinResponse>();
        Assert.True(dto!.Valido);
    }

    [Fact]
    public async Task ValidarPin_FalhaQuandoIncorreto()
    {
        using var factory = NewFactory();
        var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/validar-pin", new ValidarPinRequest { Pin = "9999" });
        var dto = await resp.Content.ReadFromJsonAsync<ValidarPinResponse>();
        Assert.False(dto!.Valido);
    }
}

public class PdvApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public PdvApiFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
