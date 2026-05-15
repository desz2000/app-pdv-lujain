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

        resp = await client.PostAsJsonAsync($"/api/comandas/{numero}/itens", new AdicionarItemRequest
        {
            Descricao = "Outro",
            Valor = 1m
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
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
