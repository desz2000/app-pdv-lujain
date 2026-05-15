using ClosedXML.Excel;
using RestaurantePDV.API.Services;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;

namespace RestaurantePDV.Tests;

public class ExcelRelatorioServiceTests
{
    [Fact]
    public void GerarRelatorio_GeraXlsxValido()
    {
        var rel = new RelatorioDiarioDto
        {
            Data = new DateTime(2025, 5, 15),
            TotalComandas = 2,
            Faturamento = 100m,
            TicketMedio = 50m,
            PorFormaPagamento = new()
            {
                new TotalPorFormaPagamentoDto { FormaPagamento = FormaPagamento.Pix, Quantidade = 1, Total = 60m },
                new TotalPorFormaPagamentoDto { FormaPagamento = FormaPagamento.Dinheiro, Quantidade = 1, Total = 40m }
            },
            PorHora = new()
            {
                new FaturamentoPorHoraDto { Hora = 12, Total = 60m },
                new FaturamentoPorHoraDto { Hora = 13, Total = 40m }
            },
            TopProdutos = new()
            {
                new TopProdutoDto { Descricao = "Prato", Quantidade = 2, Total = 90m }
            }
        };

        var svc = new ExcelRelatorioService();
        var bytes = svc.GerarRelatorioDiario(rel);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        Assert.Contains(wb.Worksheets, w => w.Name == "Resumo");
        Assert.Contains(wb.Worksheets, w => w.Name == "Forma de Pagamento");
        Assert.Contains(wb.Worksheets, w => w.Name == "Por Hora");
        Assert.Contains(wb.Worksheets, w => w.Name == "Top Produtos");
    }
}
