using System.ComponentModel.DataAnnotations;

namespace RestaurantePDV.Contracts;

public class ValidarPinRequest
{
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Pin { get; set; } = string.Empty;
}

public class ValidarPinResponse
{
    public bool Valido { get; set; }
}
