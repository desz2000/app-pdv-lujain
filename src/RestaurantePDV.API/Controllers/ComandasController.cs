using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;
using RestaurantePDV.Data;

namespace RestaurantePDV.API.Controllers;

[ApiController]
[Route("api/comandas")]
public class ComandasController : ControllerBase
{
    private readonly AppDbContext _db;

    public ComandasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{numero:int}")]
    public async Task<ActionResult<ComandaDto>> Obter(int numero)
    {
        // Prefere a comanda aberta; se nao houver, devolve a mais recente (pra mostrar fechada
        // com botão "Reabrir" no caixa).
        var comanda = await _db.Comandas
            .AsNoTracking()
            .Include(c => c.Itens)
            .Where(c => c.Numero == numero && c.Status == StatusComanda.Aberta)
            .FirstOrDefaultAsync();

        if (comanda is null)
        {
            comanda = await _db.Comandas
                .AsNoTracking()
                .Include(c => c.Itens)
                .Where(c => c.Numero == numero)
                .OrderByDescending(c => c.AbertaEm)
                .FirstOrDefaultAsync();
        }

        if (comanda is null)
        {
            return NotFound();
        }
        return Ok(ToDto(comanda));
    }

    [HttpGet]
    public async Task<ActionResult<List<ComandaDto>>> Listar([FromQuery] StatusComanda? status = null)
    {
        IQueryable<Comanda> q = _db.Comandas.AsNoTracking().Include(c => c.Itens);
        if (status.HasValue)
        {
            q = q.Where(c => c.Status == status.Value);
        }
        var comandas = await q.OrderByDescending(c => c.AbertaEm).ToListAsync();
        return Ok(comandas.Select(ToDto).ToList());
    }

    [HttpPost("{numero:int}/itens")]
    public async Task<ActionResult<ComandaDto>> AdicionarItem(int numero, [FromBody] AdicionarItemRequest request)
    {
        if (numero <= 0)
        {
            return BadRequest("Número da comanda inválido.");
        }

        if (request.Quantidade < 1)
        {
            return BadRequest("Quantidade deve ser maior ou igual a 1.");
        }

        Produto? produto = null;
        if (request.ProdutoId.HasValue)
        {
            produto = await _db.Produtos.FindAsync(request.ProdutoId.Value);
            if (produto is null)
            {
                return BadRequest($"Produto {request.ProdutoId} não encontrado.");
            }
        }

        // Resolve descricao e valor total da linha conforme o tipo de item.
        string descricao;
        decimal valorTotalLinha;
        if (produto is not null && produto.Tipo == TipoProduto.PrecoFixo)
        {
            // Preço fixo: usa o preço do cadastro multiplicado pela quantidade.
            // Operador só informa a quantidade (default 1).
            descricao = produto.Nome;
            valorTotalLinha = produto.Preco * request.Quantidade;
        }
        else
        {
            // Produto por kilo (varia por peso) ou item avulso: precisa do valor manual.
            if (request.Valor is null || request.Valor <= 0)
            {
                return BadRequest("Valor é obrigatório para item por kilo ou avulso.");
            }
            valorTotalLinha = request.Valor.Value * request.Quantidade;
            if (produto is not null)
            {
                descricao = produto.Nome;
            }
            else if (!string.IsNullOrWhiteSpace(request.Descricao))
            {
                descricao = request.Descricao.Trim();
            }
            else
            {
                descricao = request.Origem == OrigemItem.Balanca ? "Prato (balança)" : "Item";
            }
        }

        // Procura a comanda *aberta* com esse numero. Se a anterior ja foi fechada/cancelada,
        // cria uma nova — o cartao fisico foi devolvido e reusado por um novo cliente.
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero && c.Status == StatusComanda.Aberta);

        if (comanda is null)
        {
            comanda = new Comanda
            {
                Numero = numero,
                Status = StatusComanda.Aberta,
                AbertaEm = DateTime.UtcNow
            };
            _db.Comandas.Add(comanda);
        }

        var item = new ItemComanda
        {
            ProdutoId = produto?.Id,
            Descricao = descricao,
            Quantidade = request.Quantidade,
            Valor = valorTotalLinha,
            AdicionadoEm = DateTime.UtcNow,
            Origem = request.Origem
        };
        comanda.Itens.Add(item);
        comanda.ValorTotal = comanda.Itens.Sum(i => i.Valor);
        await _db.SaveChangesAsync();

        return Ok(ToDto(comanda));
    }

    [HttpDelete("{numero:int}/itens/{itemId:int}")]
    public async Task<ActionResult<ComandaDto>> RemoverItem(int numero, int itemId)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero && c.Status == StatusComanda.Aberta);
        if (comanda is null)
        {
            return NotFound();
        }
        var item = comanda.Itens.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return NotFound();
        }
        comanda.Itens.Remove(item);
        _db.ItensComanda.Remove(item);
        comanda.ValorTotal = comanda.Itens.Sum(i => i.Valor);
        await _db.SaveChangesAsync();
        return Ok(ToDto(comanda));
    }

    [HttpPost("{numero:int}/fechar")]
    public async Task<ActionResult<ComandaDto>> Fechar(int numero, [FromBody] FecharComandaRequest request)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero && c.Status == StatusComanda.Aberta);
        if (comanda is null)
        {
            return NotFound();
        }
        if (comanda.Itens.Count == 0)
        {
            return BadRequest("Comanda sem itens não pode ser fechada.");
        }

        comanda.FormaPagamento = request.FormaPagamento;
        comanda.Status = StatusComanda.Fechada;
        comanda.FechadaEm = DateTime.UtcNow;
        comanda.ValorTotal = comanda.Itens.Sum(i => i.Valor);
        await _db.SaveChangesAsync();
        return Ok(ToDto(comanda));
    }

    [HttpPost("{numero:int}/cancelar")]
    public async Task<ActionResult<ComandaDto>> Cancelar(int numero)
    {
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero && c.Status == StatusComanda.Aberta);
        if (comanda is null)
        {
            return NotFound();
        }
        comanda.Status = StatusComanda.Cancelada;
        comanda.FechadaEm = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(ToDto(comanda));
    }

    [HttpPost("{numero:int}/reabrir")]
    public async Task<ActionResult<ComandaDto>> Reabrir(int numero)
    {
        // Se ja existe uma aberta com esse numero, nao deixa reabrir outra do historico
        // (senao o cartao fisico teria duas comandas "vivas" simultaneamente).
        var jaAberta = await _db.Comandas
            .AsNoTracking()
            .AnyAsync(c => c.Numero == numero && c.Status == StatusComanda.Aberta);
        if (jaAberta)
        {
            return BadRequest($"Já existe uma comanda {numero} aberta. Feche-a antes de reabrir uma anterior.");
        }

        // Pega a mais recentemente fechada/cancelada com esse numero.
        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .Where(c => c.Numero == numero && c.Status != StatusComanda.Aberta)
            .OrderByDescending(c => c.FechadaEm)
            .ThenByDescending(c => c.AbertaEm)
            .FirstOrDefaultAsync();
        if (comanda is null)
        {
            return NotFound();
        }

        comanda.Status = StatusComanda.Aberta;
        comanda.FechadaEm = null;
        comanda.FormaPagamento = null;
        comanda.ValorTotal = comanda.Itens.Sum(i => i.Valor);
        await _db.SaveChangesAsync();
        return Ok(ToDto(comanda));
    }

    private static ComandaDto ToDto(Comanda c) => new()
    {
        Id = c.Id,
        Numero = c.Numero,
        Status = c.Status,
        AbertaEm = c.AbertaEm,
        FechadaEm = c.FechadaEm,
        FormaPagamento = c.FormaPagamento,
        ValorTotal = c.ValorTotal,
        Itens = c.Itens
            .OrderBy(i => i.AdicionadoEm)
            .Select(i => new ItemComandaDto
            {
                Id = i.Id,
                ProdutoId = i.ProdutoId,
                Descricao = i.Descricao,
                Quantidade = i.Quantidade,
                Valor = i.Valor,
                AdicionadoEm = i.AdicionadoEm,
                Origem = i.Origem
            })
            .ToList()
    };
}
