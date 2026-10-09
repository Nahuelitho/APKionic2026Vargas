using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { codigo = "datos_obligatorios", mensaje = "Ingresá el email y la contraseña." });

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await AmbitoService.UsuariosConRoles(db)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (usuario is null || !usuario.Activo || !PasswordService.Verificar(request.Password, usuario.PasswordHash))
            return Unauthorized(new { codigo = "credenciales_invalidas", mensaje = "El email o la contraseña son incorrectos." });

        var ambito = AmbitoService.Elegir(usuario, request.EmpresaId);
        if (!AmbitoService.Valido(usuario, ambito) || (request.EmpresaId is not null && ambito.Rol != "superadmin" && ambito.EmpresaId != request.EmpresaId))
            return Unauthorized(new { codigo = "ambito_invalido", mensaje = "No tiene una membresia activa en esa empresa." });
        return Ok(await CrearSesion(usuario, ambito, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { codigo = "refresh_invalido", mensaje = "La sesión guardada no es válida." });

        var hash = TokenService.HashToken(request.RefreshToken);
        var tokenAnterior = await db.RefreshTokens.Include(x => x.Usuario).ThenInclude(x => x.UsuarioRoles).ThenInclude(x => x.Rol)
            .Include(x => x.Usuario).ThenInclude(x => x.UsuarioRoles).ThenInclude(x => x.Empresa)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (tokenAnterior is null || tokenAnterior.RevocadoEn is not null || tokenAnterior.ExpiraEn <= DateTimeOffset.UtcNow ||
            tokenAnterior.VersionAmbito != 1 || !AmbitoService.Valido(tokenAnterior.Usuario, new(tokenAnterior.RolSesion, tokenAnterior.EmpresaId)))
            return Unauthorized(new { codigo = "refresh_invalido", mensaje = "La sesión guardada venció. Iniciá sesión nuevamente." });

        tokenAnterior.RevocadoEn = DateTimeOffset.UtcNow;
        try { return Ok(await CrearSesion(tokenAnterior.Usuario, new(tokenAnterior.RolSesion, tokenAnterior.EmpresaId), cancellationToken)); }
        catch (DbUpdateConcurrencyException) { return Unauthorized(new { codigo = "refresh_invalido", mensaje = "La sesion ya fue renovada." }); }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var idTexto = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(idTexto, out var id))
        {
            var activos = await db.RefreshTokens.Where(x => x.UsuarioId == id && x.RevocadoEn == null).ToListAsync(cancellationToken);
            activos.ForEach(x => x.RevocadoEn = DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    private async Task<SesionResponse> CrearSesion(Usuario usuario, AmbitoSesion ambito, CancellationToken cancellationToken)
    {
        var refreshToken = TokenService.CrearRefreshToken();
        var hash = TokenService.HashToken(refreshToken);
        var (accessToken, expira) = tokens.CrearAccessToken(usuario, ambito, hash);
        db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuario.Id,
            EmpresaId = ambito.EmpresaId, RolSesion = ambito.Rol, VersionAmbito = 1,
            TokenHash = hash,
            CreadoEn = DateTimeOffset.UtcNow,
            ExpiraEn = DateTimeOffset.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync(cancellationToken);
        return new SesionResponse(accessToken, refreshToken, expira, new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, ambito.Rol, ambito.EmpresaId));
    }
}

public sealed record LoginRequest(string Email, string Password, long? EmpresaId = null);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UsuarioResponse(long Id, string Nombre, string Email, string? Rol, long? EmpresaId = null);
public sealed record SesionResponse(string AccessToken, string RefreshToken, DateTime ExpiraUtc, UsuarioResponse Usuario);
