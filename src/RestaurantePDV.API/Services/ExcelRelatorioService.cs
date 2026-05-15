using ClosedXML.Excel;
using RestaurantePDV.Contracts;

namespace RestaurantePDV.API.Services;

public interface IExcelRelatorioService
{
    byte[] GerarRelatorioDiario(RelatorioDiarioDto relatorio);
}

public class ExcelRelatorioService : IExcelRelatorioService
{
    public byte[] GerarRelatorioDiario(RelatorioDiarioDto relatorio)
    {
        using var workbook = new XLWorkbook();

        AdicionarAbaResumo(workbook, relatorio);
        AdicionarAbaFormaPagamento(workbook, relatorio);
        AdicionarAbaPorHora(workbook, relatorio);
        AdicionarAbaTopProdutos(workbook, relatorio);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void AdicionarAbaResumo(XLWorkbook wb, RelatorioDiarioDto r)
    {
        var ws = wb.Worksheets.Add("Resumo");
        ws.Cell(1, 1).Value = "Relatório Diário — PDV Lujain";
        ws.Range(1, 1, 1, 4).Merge().Style.Font.SetBold().Font.SetFontSize(14);

        ws.Cell(3, 1).Value = "Data";
        ws.Cell(3, 2).Value = r.Data.ToString("dd/MM/yyyy");
        ws.Cell(4, 1).Value = "Total de comandas";
        ws.Cell(4, 2).Value = r.TotalComandas;
        ws.Cell(5, 1).Value = "Faturamento";
        ws.Cell(5, 2).Value = r.Faturamento;
        ws.Cell(5, 2).Style.NumberFormat.Format = "R$ #,##0.00";
        ws.Cell(6, 1).Value = "Ticket médio";
        ws.Cell(6, 2).Value = r.TicketMedio;
        ws.Cell(6, 2).Style.NumberFormat.Format = "R$ #,##0.00";

        ws.Range(3, 1, 6, 1).Style.Font.SetBold();
        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaFormaPagamento(XLWorkbook wb, RelatorioDiarioDto r)
    {
        var ws = wb.Worksheets.Add("Forma de Pagamento");
        ws.Cell(1, 1).Value = "Forma de pagamento";
        ws.Cell(1, 2).Value = "Quantidade";
        ws.Cell(1, 3).Value = "Total";
        ws.Range(1, 1, 1, 3).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

        var row = 2;
        foreach (var item in r.PorFormaPagamento)
        {
            ws.Cell(row, 1).Value = FormatarFormaPagamento(item.FormaPagamento);
            ws.Cell(row, 2).Value = item.Quantidade;
            ws.Cell(row, 3).Value = item.Total;
            ws.Cell(row, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            row++;
        }

        if (r.PorFormaPagamento.Count > 0)
        {
            ws.Cell(row, 1).Value = "Total";
            ws.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row - 1})";
            ws.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row - 1})";
            ws.Cell(row, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Range(row, 1, row, 3).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightYellow);
        }

        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaPorHora(XLWorkbook wb, RelatorioDiarioDto r)
    {
        var ws = wb.Worksheets.Add("Por Hora");
        ws.Cell(1, 1).Value = "Hora";
        ws.Cell(1, 2).Value = "Total";
        ws.Range(1, 1, 1, 2).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

        var row = 2;
        foreach (var item in r.PorHora)
        {
            ws.Cell(row, 1).Value = $"{item.Hora:00}:00";
            ws.Cell(row, 2).Value = item.Total;
            ws.Cell(row, 2).Style.NumberFormat.Format = "R$ #,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private static void AdicionarAbaTopProdutos(XLWorkbook wb, RelatorioDiarioDto r)
    {
        var ws = wb.Worksheets.Add("Top Produtos");
        ws.Cell(1, 1).Value = "Produto";
        ws.Cell(1, 2).Value = "Quantidade";
        ws.Cell(1, 3).Value = "Total";
        ws.Range(1, 1, 1, 3).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

        var row = 2;
        foreach (var item in r.TopProdutos)
        {
            ws.Cell(row, 1).Value = item.Descricao;
            ws.Cell(row, 2).Value = item.Quantidade;
            ws.Cell(row, 3).Value = item.Total;
            ws.Cell(row, 3).Style.NumberFormat.Format = "R$ #,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    public static string FormatarFormaPagamento(RestaurantePDV.Core.FormaPagamento fp) => fp switch
    {
        RestaurantePDV.Core.FormaPagamento.Dinheiro => "Dinheiro",
        RestaurantePDV.Core.FormaPagamento.Debito => "Débito",
        RestaurantePDV.Core.FormaPagamento.Credito => "Crédito",
        RestaurantePDV.Core.FormaPagamento.Pix => "Pix",
        RestaurantePDV.Core.FormaPagamento.ValeRefeicao => "Vale Refeição",
        RestaurantePDV.Core.FormaPagamento.ValeAlimentacao => "Vale Alimentação",
        _ => fp.ToString()
    };
}
