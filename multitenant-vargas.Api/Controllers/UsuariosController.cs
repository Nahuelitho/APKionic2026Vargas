using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,administrador")]
[Route("api/usuarios")]
public sealed class UsuariosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var empresa = AmbitoService.EmpresaId(User);
        var global = AmbitoService.Global(User);
        var usuarios = await AmbitoService.UsuariosConRoles(db).AsNoTracking()
            .Where(x => global || x.UsuarioRoles.Any(r => r.EmpresaId == empresa && empresa != null))
            .OrderBy(x => x.Nombre).ToListAsync(ct);
        return Ok(usuarios.Select(x =>
        {
            var roles = x.UsuarioRoles.Where(r => global || r.EmpresaId == empresa).ToArray();
            var rol = roles.OrderBy(r => r.EmpresaId).FirstOrDefault();
            return new UsuarioAdminResponse(x.Id, x.Nombre, x.Email, x.Activo, rol?.RolId, rol?.Rol.Codigo, rol?.EmpresaId,
                roles.Select(r => new MembresiaResponse(r.RolId, r.Rol.Codigo, r.EmpresaId)).ToArray());
        }));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> ListarRoles(CancellationToken ct) => Ok(await db.Roles.AsNoTracking()
        .Where(x => x.Activo && (AmbitoService.Global(User) || x.Codigo != "superadmin"))
        .OrderBy(x => x.Nombre).Select(x => new RolResponse(x.Id, x.Nombre, x.Codigo)).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > 120 || email.Length > 160 ||
            !System.Net.Mail.MailAddress.TryCreate(email, out _) || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return BadRequest(new { codigo = "usuario_invalido", mensaje = "Complete nombre, email y una contrasena de al menos 8 caracteres." });
        var empresa = AmbitoService.Global(User) ? request.EmpresaId : AmbitoService.EmpresaId(User);
        if (!await RolPermitido(request.RolId, empresa, request.EmpresaId, ct)) return RolInvalido();
        if (await db.Usuarios.AnyAsync(x => x.Email == email, ct))
            return Conflict(new { codigo = "email_existente", mensaje = "Ese email no esta disponible." });
        var usuario = new Usuario { Nombre = request.Nombre.Trim(), Email = email, PasswordHash = PasswordService.Hash(request.Password) };
        if (request.RolId is not null) usuario.UsuarioRoles.Add(new UsuarioRol { RolId = request.RolId.Value, EmpresaId = empresa });
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        return Created($"/api/usuarios/{usuario.Id}", new { usuario.Id });
    }

    [HttpPut("{id:long}/rol")]
    public async Task<IActionResult> AsignarRol(long id, AsignarRolRequest request, CancellationToken ct)
    {
        var global = AmbitoService.Global(User);
        var empresa = global ? request.EmpresaId : AmbitoService.EmpresaId(User);
        var usuario = await AmbitoService.UsuariosConRoles(db).FirstOrDefaultAsync(x => x.Id == id &&
            (global || x.UsuarioRoles.Any(r => r.EmpresaId == empresa && empresa != null)), ct);
        if (usuario is null) return NotFound(new { codigo = "usuario_no_encontrado", mensaje = "No se encontro el usuario." });
        if (!await RolPermitido(request.RolId, empresa, request.EmpresaId, ct)) return RolInvalido();
        // Company admins cannot alter a global administrator or their own authorization.
        if (!global && (id == AmbitoService.UsuarioId(User) || usuario.UsuarioRoles.Any(r => r.EmpresaId == null))) return Forbid();
        var existentes = usuario.UsuarioRoles.Where(x => x.EmpresaId == empresa).ToList();
        db.UsuarioRoles.RemoveRange(existentes);
        if (request.RolId is not null)
        {
            if (existentes.Count > 0)
            {
                var rol = existentes[0]; db.Entry(rol).State = EntityState.Unchanged; rol.RolId = request.RolId.Value;
            }
            else db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = id, RolId = request.RolId.Value, EmpresaId = empresa });
        }
        var tokens = await db.RefreshTokens.Where(x => x.UsuarioId == id && x.RevocadoEn == null).ToListAsync(ct);
        tokens.ForEach(x => x.RevocadoEn = DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> RolPermitido(long? rolId, long? empresa, long? solicitado, CancellationToken ct)
    {
        if (!AmbitoService.Global(User) && (empresa is null || (solicitado is not null && solicitado != empresa) || rolId is null)) return false;
        if (rolId is null) return empresa is null || await db.Empresas.AnyAsync(x => x.Id == empresa && x.Activo, ct);
        var rol = await db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == rolId && x.Activo, ct);
        if (rol?.Codigo == "superadmin") return AmbitoService.Global(User) && empresa is null;
        return rol?.Codigo is "administrador" or "vendedor" or "caja" && empresa is not null &&
            await db.Empresas.AnyAsync(x => x.Id == empresa && x.Activo, ct);
    }

    private BadRequestObjectResult RolInvalido() => BadRequest(new { codigo = "rol_invalido", mensaje = "El rol y la empresa no estan permitidos en su ambito." });
}

public sealed record MembresiaResponse(long RolId, string Rol, long? EmpresaId);
public sealed record UsuarioAdminResponse(long Id, string Nombre, string Email, bool Activo, long? RolId, string? Rol, long? EmpresaId, IReadOnlyList<MembresiaResponse> Membresias);
public sealed record RolResponse(long Id, string Nombre, string Codigo);
public sealed record CrearUsuarioRequest(string Nombre, string Email, string Password, long? RolId, long? EmpresaId = null);
public sealed record AsignarRolRequest(long? RolId, long? EmpresaId = null);
