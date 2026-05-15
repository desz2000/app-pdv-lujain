namespace RestaurantePDV.Core;

public class Comanda
{
    public int Id { get; set; }

    public int Numero { get; set; }

    public StatusComanda Status { get; set; } = StatusComanda.Aberta;

    public DateTime AbertaEm { get; set; } = DateTime.UtcNow;

    public DateTime? FechadaEm { get; set; }

    public FormaPagamento? FormaPagamento { get; set; }

    public decimal ValorTotal { get; set; }

    public List<ItemComanda> Itens { get; set; } = new();
}
