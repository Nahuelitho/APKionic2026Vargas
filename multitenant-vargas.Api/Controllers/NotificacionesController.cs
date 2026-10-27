using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/notificaciones")]
[Authorize]
public sealed class NotificacionesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var usuarioId = AmbitoService.UsuarioId(User);
        var notificaciones = await db.Notificaciones.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Take(50)
            .Select(x => new NotificacionResponse(x.Id, x.Titulo, x.Mensaje, x.PedidoId, x.Leida,
                DateTime.SpecifyKind(x.Fecha, DateTimeKind.Utc)))
            .ToListAsync(ct);
        return Ok(new { notificaciones });
    }

    [HttpGet("no-leidas")]
    public async Task<IActionResult> NoLeidas(CancellationToken ct)
    {
        var usuarioId = AmbitoService.UsuarioId(User);
        var total = await db.Notificaciones.AsNoTracking()
            .CountAsync(x => x.UsuarioId == usuarioId && !x.Leida, ct);
        return Ok(new { total });
    }

    [HttpPut("{id:long}/leida")]
    public async Task<IActionResult> MarcarLeida(long id, CancellationToken ct)
    {
        var notificacion = await db.Notificaciones.FirstOrDefaultAsync(
            x => x.Id == id && x.UsuarioId == AmbitoService.UsuarioId(User), ct);
        if (notificacion is null) return NotFound();
        notificacion.Leida = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("leidas")]
    public async Task<IActionResult> MarcarTodas(CancellationToken ct)
    {
        var usuarioId = AmbitoService.UsuarioId(User);
        await db.Notificaciones
            .Where(x => x.UsuarioId == usuarioId && !x.Leida)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.Leida, true), ct);
        return NoContent();
    }
}

public sealed record NotificacionResponse(long Id, string Titulo, string Mensaje, long? PedidoId, bool Leida, DateTime Fecha);