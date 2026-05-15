namespace RestaurantePDV.Desktop.Services;

public class PinState
{
    public string? Pin { get; private set; }
    public bool Autenticado => !string.IsNullOrWhiteSpace(Pin);

    public event Action? Mudou;

    public void Definir(string pin)
    {
        Pin = pin;
        Mudou?.Invoke();
    }

    public void Limpar()
    {
        Pin = null;
        Mudou?.Invoke();
    }
}
