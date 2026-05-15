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
    public int Quantidade { get; set; } = 1;
    public decimal Valor { get; set; }
    public DateTime AdicionadoEm { get; set; }
    public OrigemItem Origem { get; set; }
}

public class AdicionarItemRequest
{
    public int? ProdutoId { get; set; }

    [StringLength(200)]
    public string? Descricao { get; set; }

    // Para produto preco fixo: ignorado (calculado como produto.Preco * Quantidade).
    // Para produto por kilo ou item avulso: obrigatorio e usado como total da linha.
    public decimal? Valor { get; set; }

    [Range(1, 1000)]
    public int Quantidade { get; set; } = 1;

    public OrigemItem Origem { get; set; } = OrigemItem.Caixa;
}

public class FecharComandaRequest
{
    public FormaPagamento FormaPagamento { get; set; }
}
