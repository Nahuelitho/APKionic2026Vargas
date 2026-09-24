using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Authorize(Roles = "administrador")]
[Route("api/usuarios")]
public sealed class UsuariosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken) => Ok(await db.Usuarios.AsNoTracking()
        .Include(x => x.Rol).OrderBy(x => x.Nombre)
        .Select(x => new UsuarioAdminResponse(x.Id, x.Nombre, x.Email, x.Activo, x.RolId, x.Rol == null ? null : x.Rol.Codigo))
        .ToListAsync(cancellationToken));

    [HttpGet("roles")]
    public async Task<IActionResult> ListarRoles(CancellationToken cancellationToken) => Ok(await db.Roles.AsNoTracking()
        .Where(x => x.Activo).OrderBy(x => x.Nombre)
        .Select(x => new RolResponse(x.Id, x.Nombre, x.Codigo)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { codigo = "usuario_invalido", mensaje = "Completá nombre, email y una contraseña de al menos 8 caracteres." });
        if (await db.Usuarios.AnyAsync(x => x.Email == email, cancellationToken))
            return Conflict(new { codigo = "email_existente", mensaje = "Ya existe un usuario con ese email." });
        if (request.RolId is not null && !await db.Roles.AnyAsync(x => x.Id == request.RolId && x.Activo, cancellationToken))
            return BadRequest(new { codigo = "rol_invalido", mensaje = "El rol seleccionado no está disponible." });

        var usuario = new Usuario { Nombre = request.Nombre.Trim(), Email = email, PasswordHash = PasswordService.Hash(request.Password), RolId = request.RolId, Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/usuarios/{usuario.Id}", new { usuario.Id });
    }

    [HttpPut("{id:long}/rol")]
    public async Task<IActionResult> AsignarRol(long id, AsignarRolRequest request, CancellationToken cancellationToken)
    {
        var usuario = await db.Usuarios.FindAsync([id], cancellationToken);
        if (usuario is null) return NotFound(new { codigo = "usuario_no_encontrado", mensaje = "No se encontró el usuario." });
        if (request.RolId is not null && !await db.Roles.AnyAsync(x => x.Id == request.RolId && x.Activo, cancellationToken))
            return BadRequest(new { codigo = "rol_invalido", mensaje = "El rol seleccionado no está disponible." });
        usuario.RolId = request.RolId;
        var tokens = await db.RefreshTokens.Where(x => x.UsuarioId == id && x.RevocadoEn == null).ToListAsync(cancellationToken);
        tokens.ForEach(x => x.RevocadoEn = DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public sealed record UsuarioAdminResponse(long Id, string Nombre, string Email, bool Activo, long? RolId, string? Rol);
public sealed record RolResponse(long Id, string Nombre, string Codigo);
public sealed record CrearUsuarioRequest(string Nombre, string Email, string Password, long? RolId);
public sealed record AsignarRolRequest(long? RolId);
