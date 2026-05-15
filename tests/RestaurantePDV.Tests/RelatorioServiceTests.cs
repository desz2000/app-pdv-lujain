using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestaurantePDV.API.Services;
using RestaurantePDV.Core;
using RestaurantePDV.Data;

namespace RestaurantePDV.Tests;

public class RelatorioServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public RelatorioServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    private AppDbContext NewContext() => new(_options);

    [Fact]
    public async Task RelatorioDiario_AgregaCorretamente()
    {
        var hoje = DateTime.UtcNow.Date.AddHours(12);

        await using (var seed = NewContext())
        {
            seed.Comandas.AddRange(
                new Comanda
                {
                    Numero = 1,
                    Status = StatusComanda.Fechada,
                    AbertaEm = hoje,
                    FechadaEm = hoje,
                    FormaPagamento = FormaPagamento.Pix,
                    ValorTotal = 30m,
                    Itens = new() { new ItemComanda { Descricao = "Prato", Valor = 30m, Origem = OrigemItem.Balanca, AdicionadoEm = hoje } }
                },
                new Comanda
                {
                    Numero = 2,
                    Status = StatusComanda.Fechada,
                    AbertaEm = hoje,
                    FechadaEm = hoje,
                    FormaPagamento = FormaPagamento.Dinheiro,
                    ValorTotal = 50m,
                    Itens = new()
                    {
                        new ItemComanda { Descricao = "Prato", Valor = 40m, Origem = OrigemItem.Balanca, AdicionadoEm = hoje },
                        new ItemComanda { Descricao = "Suco", Valor = 10m, Origem = OrigemItem.Caixa, AdicionadoEm = hoje }
                    }
                },
                new Comanda
                {
                    Numero = 3,
                    Status = StatusComanda.Aberta,
                    AbertaEm = hoje,
                    ValorTotal = 5m,
                    Itens = new() { new ItemComanda { Descricao = "Suco", Valor = 5m, Origem = OrigemItem.Caixa, AdicionadoEm = hoje } }
                });
            await seed.SaveChangesAsync();
        }

        await using var db = NewContext();
        var service = new RelatorioService(db);
        var rel = await service.ObterRelatorioDoDiaAsync(hoje.Date);

        Assert.Equal(2, rel.TotalComandas);
        Assert.Equal(80m, rel.Faturamento);
        Assert.Equal(40m, rel.TicketMedio);

        Assert.Equal(2, rel.PorFormaPagamento.Count);
        Assert.Contains(rel.PorFormaPagamento, p => p.FormaPagamento == FormaPagamento.Dinheiro && p.Total == 50m);
        Assert.Contains(rel.PorFormaPagamento, p => p.FormaPagamento == FormaPagamento.Pix && p.Total == 30m);

        Assert.Contains(rel.TopProdutos, t => t.Descricao == "Prato" && t.Quantidade == 2 && t.Total == 70m);
        Assert.Contains(rel.TopProdutos, t => t.Descricao == "Suco" && t.Quantidade == 1 && t.Total == 10m);
    }

    [Fact]
    public async Task RelatorioDiario_SemDados_RetornaZeros()
    {
        await using var db = NewContext();
        var service = new RelatorioService(db);

        var rel = await service.ObterRelatorioDoDiaAsync(DateTime.UtcNow.Date);

        Assert.Equal(0, rel.TotalComandas);
        Assert.Equal(0m, rel.Faturamento);
        Assert.Equal(0m, rel.TicketMedio);
        Assert.Empty(rel.PorFormaPagamento);
        Assert.Empty(rel.TopProdutos);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
