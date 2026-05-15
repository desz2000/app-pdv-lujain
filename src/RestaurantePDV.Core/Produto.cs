using System.ComponentModel.DataAnnotations;

namespace RestaurantePDV.Core;

public class Produto
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    public decimal Preco { get; set; }

    public TipoProduto Tipo { get; set; } = TipoProduto.PrecoFixo;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
