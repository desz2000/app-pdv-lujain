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
        var comanda = await _db.Comandas
            .AsNoTracking()
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero);
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

        var comanda = await _db.Comandas
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Numero == numero);

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

        if (comanda.Status != StatusComanda.Aberta)
        {
            return BadRequest($"Comanda {numero} já está {comanda.Status.ToString().ToLowerInvariant()}.");
        }

        string descricao;
        if (request.ProdutoId.HasValue)
        {
            var produto = await _db.Produtos.FindAsync(request.ProdutoId.Value);
            if (produto is null)
            {
                return BadRequest($"Produto {request.ProdutoId} não encontrado.");
            }
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

        var item = new ItemComanda
        {
            ProdutoId = request.ProdutoId,
            Descricao = descricao,
            Valor = request.Valor,
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
            .FirstOrDefaultAsync(c => c.Numero == numero);
        if (comanda is null)
        {
            return NotFound();
        }
        if (comanda.Status != StatusComanda.Aberta)
        {
            return BadRequest($"Comanda {numero} já está {comanda.Status.ToString().ToLowerInvariant()}.");
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
            .FirstOrDefaultAsync(c => c.Numero == numero);
        if (comanda is null)
        {
            return NotFound();
        }
        if (comanda.Status != StatusComanda.Aberta)
        {
            return BadRequest($"Comanda {numero} já está {comanda.Status.ToString().ToLowerInvariant()}.");
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
            .FirstOrDefaultAsync(c => c.Numero == numero);
        if (comanda is null)
        {
            return NotFound();
        }
        if (comanda.Status == StatusComanda.Fechada)
        {
            return BadRequest("Comanda já fechada não pode ser cancelada.");
        }
        comanda.Status = StatusComanda.Cancelada;
        comanda.FechadaEm = DateTime.UtcNow;
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
                Valor = i.Valor,
                AdicionadoEm = i.AdicionadoEm,
                Origem = i.Origem
            })
            .ToList()
    };
}
