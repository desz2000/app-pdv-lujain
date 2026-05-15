using Microsoft.AspNetCore.Mvc;
using RestaurantePDV.API.Services;
using RestaurantePDV.Contracts;

namespace RestaurantePDV.API.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly IRelatorioService _relatorios;
    private readonly IExcelRelatorioService _excel;

    public RelatoriosController(IRelatorioService relatorios, IExcelRelatorioService excel)
    {
        _relatorios = relatorios;
        _excel = excel;
    }

    [HttpGet("dia")]
    public async Task<ActionResult<RelatorioDiarioDto>> Diario([FromQuery] DateTime? data, CancellationToken ct)
    {
        var alvo = (data ?? DateTime.UtcNow).Date;
        var rel = await _relatorios.ObterRelatorioDoDiaAsync(alvo, ct);
        return Ok(rel);
    }

    [HttpGet("dia/excel")]
    public async Task<IActionResult> DiarioExcel([FromQuery] DateTime? data, CancellationToken ct)
    {
        var alvo = (data ?? DateTime.UtcNow).Date;
        var rel = await _relatorios.ObterRelatorioDoDiaAsync(alvo, ct);
        var bytes = _excel.GerarRelatorioDiario(rel);
        var nome = $"relatorio-{alvo:yyyy-MM-dd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nome);
    }
}
