using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sesion")]
public sealed class SesionController : ControllerBase
{
    [HttpGet("yo")]
    public IActionResult Yo()
    {
        var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(new UsuarioResponse(id, User.Identity!.Name!, User.FindFirstValue(ClaimTypes.Email)!, User.FindFirstValue(ClaimTypes.Role), AmbitoService.EmpresaId(User)));
    }
}
