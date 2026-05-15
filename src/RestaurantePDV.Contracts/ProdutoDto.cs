using System.ComponentModel.DataAnnotations;
using RestaurantePDV.Core;

namespace RestaurantePDV.Contracts;

public class ProdutoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public TipoProduto Tipo { get; set; }
    public bool Ativo { get; set; }
}

public class CriarProdutoRequest
{
    [Required]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal Preco { get; set; }

    public TipoProduto Tipo { get; set; } = TipoProduto.PrecoFixo;
}

public class AtualizarProdutoRequest
{
    [Required]
    [StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal Preco { get; set; }

    public TipoProduto Tipo { get; set; }

    public bool Ativo { get; set; }
}
