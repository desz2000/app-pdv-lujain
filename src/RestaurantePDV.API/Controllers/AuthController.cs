using Microsoft.AspNetCore.Mvc;
using RestaurantePDV.Contracts;

namespace RestaurantePDV.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("validar-pin")]
    public ActionResult<ValidarPinResponse> ValidarPin([FromBody] ValidarPinRequest request)
    {
        var expected = _config["App:Pin"] ?? string.Empty;
        var valido = !string.IsNullOrWhiteSpace(expected) && request.Pin == expected;
        return Ok(new ValidarPinResponse { Valido = valido });
    }
}
