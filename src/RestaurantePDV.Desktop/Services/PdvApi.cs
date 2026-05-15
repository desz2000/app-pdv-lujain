using System.Net.Http.Headers;
using System.Net.Http.Json;
using RestaurantePDV.Contracts;
using RestaurantePDV.Core;

namespace RestaurantePDV.Desktop.Services;

public interface IPdvApi
{
    Task<bool> ValidarPinAsync(string pin, CancellationToken ct = default);

    Task<List<ProdutoDto>> ListarProdutosAsync(bool incluirInativos = false, CancellationToken ct = default);
    Task<ProdutoDto?> CriarProdutoAsync(CriarProdutoRequest request, CancellationToken ct = default);
    Task<ProdutoDto?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request, CancellationToken ct = default);
    Task InativarProdutoAsync(int id, CancellationToken ct = default);

    Task<ComandaDto?> ObterComandaAsync(int numero, CancellationToken ct = default);
    Task<ComandaDto?> AdicionarItemAsync(int numero, AdicionarItemRequest request, CancellationToken ct = default);
    Task<ComandaDto?> RemoverItemAsync(int numero, int itemId, CancellationToken ct = default);
    Task<ComandaDto?> FecharComandaAsync(int numero, FormaPagamento forma, CancellationToken ct = default);
    Task<ComandaDto?> CancelarComandaAsync(int numero, CancellationToken ct = default);
    Task<ComandaDto?> ReabrirComandaAsync(int numero, CancellationToken ct = default);

    Task<RelatorioDiarioDto?> ObterRelatorioDiarioAsync(DateTime data, CancellationToken ct = default);
    Task<byte[]?> BaixarRelatorioExcelAsync(DateTime data, CancellationToken ct = default);
}

public class PdvApi : IPdvApi
{
    private readonly HttpClient _http;
    private readonly PinState _pin;

    public PdvApi(HttpClient http, PinState pin)
    {
        _http = http;
        _pin = pin;
    }

    private void AplicarPin()
    {
        _http.DefaultRequestHeaders.Remove("X-Pin");
        if (!string.IsNullOrWhiteSpace(_pin.Pin))
        {
            _http.DefaultRequestHeaders.Add("X-Pin", _pin.Pin);
        }
    }

    public async Task<bool> ValidarPinAsync(string pin, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/validar-pin", new ValidarPinRequest { Pin = pin }, ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }
        var dto = await response.Content.ReadFromJsonAsync<ValidarPinResponse>(cancellationToken: ct);
        return dto?.Valido ?? false;
    }

    public async Task<List<ProdutoDto>> ListarProdutosAsync(bool incluirInativos = false, CancellationToken ct = default)
    {
        AplicarPin();
        var url = $"/api/produtos?incluirInativos={(incluirInativos ? "true" : "false")}";
        var lista = await _http.GetFromJsonAsync<List<ProdutoDto>>(url, ct);
        return lista ?? new List<ProdutoDto>();
    }

    public async Task<ProdutoDto?> CriarProdutoAsync(CriarProdutoRequest request, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PostAsJsonAsync("/api/produtos", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProdutoDto>(cancellationToken: ct);
    }

    public async Task<ProdutoDto?> AtualizarProdutoAsync(int id, AtualizarProdutoRequest request, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PutAsJsonAsync($"/api/produtos/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProdutoDto>(cancellationToken: ct);
    }

    public async Task InativarProdutoAsync(int id, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.DeleteAsync($"/api/produtos/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ComandaDto?> ObterComandaAsync(int numero, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.GetAsync($"/api/comandas/{numero}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<ComandaDto?> AdicionarItemAsync(int numero, AdicionarItemRequest request, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PostAsJsonAsync($"/api/comandas/{numero}/itens", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<ComandaDto?> RemoverItemAsync(int numero, int itemId, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.DeleteAsync($"/api/comandas/{numero}/itens/{itemId}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<ComandaDto?> FecharComandaAsync(int numero, FormaPagamento forma, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PostAsJsonAsync($"/api/comandas/{numero}/fechar", new FecharComandaRequest { FormaPagamento = forma }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<ComandaDto?> CancelarComandaAsync(int numero, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PostAsync($"/api/comandas/{numero}/cancelar", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<ComandaDto?> ReabrirComandaAsync(int numero, CancellationToken ct = default)
    {
        AplicarPin();
        var response = await _http.PostAsync($"/api/comandas/{numero}/reabrir", content: null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComandaDto>(cancellationToken: ct);
    }

    public async Task<RelatorioDiarioDto?> ObterRelatorioDiarioAsync(DateTime data, CancellationToken ct = default)
    {
        AplicarPin();
        var url = $"/api/relatorios/dia?data={data:yyyy-MM-dd}";
        return await _http.GetFromJsonAsync<RelatorioDiarioDto>(url, ct);
    }

    public async Task<byte[]?> BaixarRelatorioExcelAsync(DateTime data, CancellationToken ct = default)
    {
        AplicarPin();
        var url = $"/api/relatorios/dia/excel?data={data:yyyy-MM-dd}";
        var response = await _http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}
