using RestaurantePDV.Core;

namespace RestaurantePDV.Contracts;

public class RelatorioDiarioDto
{
    public DateTime Data { get; set; }
    public int TotalComandas { get; set; }
    public decimal Faturamento { get; set; }
    public decimal TicketMedio { get; set; }
    public List<TotalPorFormaPagamentoDto> PorFormaPagamento { get; set; } = new();
    public List<FaturamentoPorHoraDto> PorHora { get; set; } = new();
    public List<TopProdutoDto> TopProdutos { get; set; } = new();
}

public class TotalPorFormaPagamentoDto
{
    public FormaPagamento FormaPagamento { get; set; }
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
}

public class FaturamentoPorHoraDto
{
    public int Hora { get; set; }
    public decimal Total { get; set; }
}

public class TopProdutoDto
{
    public int? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
}
