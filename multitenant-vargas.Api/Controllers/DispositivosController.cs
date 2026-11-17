using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

/// <summary>
/// Tabla de telefonos. La app manda su token FCM al entrar y lo borra al salir.
/// </summary>
[ApiController]
[Route("api/dispositivos")]
[Authorize]
public sealed class DispositivosController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] DispositivoRequest request, CancellationToken ct)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 255)
            return BadRequest(new { codigo = "token_invalido", mensaje = "El token del telefono es obligatorio y admite hasta 255 caracteres." });

        var plataforma = string.IsNullOrWhiteSpace(request.Plataforma) ? "android" : request.Plataforma.Trim().ToLowerInvariant();
        if (plataforma.Length > 20)
            return BadRequest(new { codigo = "plataforma_invalida", mensaje = "La plataforma informada no es valida." });

        var usuarioId = AmbitoService.UsuarioId(User);
        var existente = await db.Dispositivos.FirstOrDefaultAsync(x => x.Token == token, ct);
        if (existente is null)
        {
            db.Dispositivos.Add(new Dispositivo
            {
                UsuarioId = usuarioId,
                Token = token,
                Plataforma = plataforma,
                CreadoEn = DateTime.UtcNow,
                ActualizadoEn = DateTime.UtcNow,
            });
        }
        else
        {
            // El telefono puede haber cambiado de usuario: el token queda a nombre del actual.
            existente.UsuarioId = usuarioId;
            existente.Plataforma = plataforma;
            existente.ActualizadoEn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Eliminar(string token, CancellationToken ct)
    {
        var usuarioId = AmbitoService.UsuarioId(User);
        await db.Dispositivos
            .Where(x => x.Token == token && x.UsuarioId == usuarioId)
            .ExecuteDeleteAsync(ct);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> EliminarTodos(CancellationToken ct)
    {
        var usuarioId = AmbitoService.UsuarioId(User);
        await db.Dispositivos.Where(x => x.UsuarioId == usuarioId).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}

public sealed record DispositivoRequest(string Token, string? Plataforma = null);
