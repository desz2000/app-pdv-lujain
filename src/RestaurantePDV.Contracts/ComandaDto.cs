using System.ComponentModel.DataAnnotations;
using RestaurantePDV.Core;

namespace RestaurantePDV.Contracts;

public class ComandaDto
{
    public int Id { get; set; }
    public int Numero { get; set; }
    public StatusComanda Status { get; set; }
    public DateTime AbertaEm { get; set; }
    public DateTime? FechadaEm { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }
    public decimal ValorTotal { get; set; }
    public List<ItemComandaDto> Itens { get; set; } = new();
}

public class ItemComandaDto
{
    public int Id { get; set; }
    public int? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime AdicionadoEm { get; set; }
    public OrigemItem Origem { get; set; }
}

public class AdicionarItemRequest
{
    public int? ProdutoId { get; set; }

    [StringLength(200)]
    public string? Descricao { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Valor { get; set; }

    public OrigemItem Origem { get; set; } = OrigemItem.Caixa;
}

public class FecharComandaRequest
{
    public FormaPagamento FormaPagamento { get; set; }
}
