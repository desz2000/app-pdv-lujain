using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;
using RestaurantePDV.Data;

namespace RestaurantePDV.API.Controllers;

[ApiController]
[Route("api/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProdutosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProdutoDto>>> Listar([FromQuery] bool incluirInativos = false)
    {
        IQueryable<Produto> q = _db.Produtos.AsNoTracking();
        if (!incluirInativos)
        {
            q = q.Where(p => p.Ativo);
        }
        var produtos = await q.OrderBy(p => p.Nome).ToListAsync();
        return Ok(produtos.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> Obter(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
        {
            return NotFound();
        }
        return Ok(ToDto(produto));
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoDto>> Criar([FromBody] CriarProdutoRequest request)
    {
        var produto = new Produto
        {
            Nome = request.Nome.Trim(),
            Preco = request.Preco,
            Tipo = request.Tipo,
            Ativo = true,
            CriadoEm = DateTime.UtcNow
        };
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obter), new { id = produto.Id }, ToDto(produto));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProdutoDto>> Atualizar(int id, [FromBody] AtualizarProdutoRequest request)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
        {
            return NotFound();
        }
        produto.Nome = request.Nome.Trim();
        produto.Preco = request.Preco;
        produto.Tipo = request.Tipo;
        produto.Ativo = request.Ativo;
        await _db.SaveChangesAsync();
        return Ok(ToDto(produto));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Inativar(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto is null)
        {
            return NotFound();
        }
        produto.Ativo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static ProdutoDto ToDto(Produto p) => new()
    {
        Id = p.Id,
        Nome = p.Nome,
        Preco = p.Preco,
        Tipo = p.Tipo,
        Ativo = p.Ativo
    };
}
