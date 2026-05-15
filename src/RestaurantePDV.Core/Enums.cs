namespace RestaurantePDV.Core;

public enum TipoProduto
{
    PorKilo = 0,
    PrecoFixo = 1
}

public enum StatusComanda
{
    Aberta = 0,
    Fechada = 1,
    Cancelada = 2
}

public enum FormaPagamento
{
    Dinheiro = 0,
    Debito = 1,
    Credito = 2,
    Pix = 3,
    ValeRefeicao = 4,
    ValeAlimentacao = 5
}

public enum OrigemItem
{
    Balanca = 0,
    Caixa = 1
}
