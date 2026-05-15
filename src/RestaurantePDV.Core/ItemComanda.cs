using System.ComponentModel.DataAnnotations;

namespace RestaurantePDV.Core;

public class ItemComanda
{
    public int Id { get; set; }

    public int ComandaId { get; set; }
    public Comanda? Comanda { get; set; }

    public int? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [Required]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    public int Quantidade { get; set; } = 1;

    // Valor total da linha (ja inclui Quantidade — ex.: 3 cocas a R$ 5 cada => Valor=15).
    public decimal Valor { get; set; }

    public DateTime AdicionadoEm { get; set; } = DateTime.UtcNow;

    public OrigemItem Origem { get; set; } = OrigemItem.Caixa;
}
