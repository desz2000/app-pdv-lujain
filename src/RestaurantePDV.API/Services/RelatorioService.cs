using Microsoft.EntityFrameworkCore;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;
using RestaurantePDV.Data;

namespace RestaurantePDV.API.Services;

public interface IRelatorioService
{
    Task<RelatorioDiarioDto> ObterRelatorioDoDiaAsync(DateTime data, CancellationToken ct = default);
}

public class RelatorioService : IRelatorioService
{
    private readonly AppDbContext _db;

    public RelatorioService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RelatorioDiarioDto> ObterRelatorioDoDiaAsync(DateTime data, CancellationToken ct = default)
    {
        var inicio = data.Date;
        var fim = inicio.AddDays(1);

        var comandas = await _db.Comandas
            .AsNoTracking()
            .Include(c => c.Itens)
            .Where(c => c.Status == StatusComanda.Fechada
                && c.FechadaEm != null
                && c.FechadaEm >= inicio
                && c.FechadaEm < fim)
            .ToListAsync(ct);

        var faturamento = comandas.Sum(c => c.ValorTotal);
        var totalComandas = comandas.Count;
        var ticketMedio = totalComandas > 0 ? faturamento / totalComandas : 0m;

        var porFormaPagamento = comandas
            .Where(c => c.FormaPagamento.HasValue)
            .GroupBy(c => c.FormaPagamento!.Value)
            .Select(g => new TotalPorFormaPagamentoDto
            {
                FormaPagamento = g.Key,
                Quantidade = g.Count(),
                Total = g.Sum(c => c.ValorTotal)
            })
            .OrderByDescending(g => g.Total)
            .ToList();

        var porHora = comandas
            .Where(c => c.FechadaEm.HasValue)
            .GroupBy(c => c.FechadaEm!.Value.ToLocalTime().Hour)
            .Select(g => new FaturamentoPorHoraDto
            {
                Hora = g.Key,
                Total = g.Sum(c => c.ValorTotal)
            })
            .OrderBy(g => g.Hora)
            .ToList();

        var topProdutos = comandas
            .SelectMany(c => c.Itens)
            .GroupBy(i => new { i.ProdutoId, Descricao = i.Descricao ?? string.Empty })
            .Select(g => new TopProdutoDto
            {
                ProdutoId = g.Key.ProdutoId,
                Descricao = g.Key.Descricao,
                Quantidade = g.Count(),
                Total = g.Sum(i => i.Valor)
            })
            .OrderByDescending(t => t.Total)
            .Take(10)
            .ToList();

        return new RelatorioDiarioDto
        {
            Data = inicio,
            TotalComandas = totalComandas,
            Faturamento = faturamento,
            TicketMedio = ticketMedio,
            PorFormaPagamento = porFormaPagamento,
            PorHora = porHora,
            TopProdutos = topProdutos
        };
    }
}
