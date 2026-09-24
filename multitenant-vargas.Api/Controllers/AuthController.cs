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
        var usuario = await db.Usuarios.Include(x => x.Rol)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (usuario is null || !usuario.Activo || !PasswordService.Verificar(request.Password, usuario.PasswordHash))
            return Unauthorized(new { codigo = "credenciales_invalidas", mensaje = "El email o la contraseña son incorrectos." });

        return Ok(await CrearSesion(usuario, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { codigo = "refresh_invalido", mensaje = "La sesión guardada no es válida." });

        var hash = TokenService.HashToken(request.RefreshToken);
        var tokenAnterior = await db.RefreshTokens.Include(x => x.Usuario).ThenInclude(x => x.Rol)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (tokenAnterior is null || tokenAnterior.RevocadoEn is not null || tokenAnterior.ExpiraEn <= DateTimeOffset.UtcNow || !tokenAnterior.Usuario.Activo)
            return Unauthorized(new { codigo = "refresh_invalido", mensaje = "La sesión guardada venció. Iniciá sesión nuevamente." });

        tokenAnterior.RevocadoEn = DateTimeOffset.UtcNow;
        return Ok(await CrearSesion(tokenAnterior.Usuario, cancellationToken));
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

    private async Task<SesionResponse> CrearSesion(Usuario usuario, CancellationToken cancellationToken)
    {
        var (accessToken, expira) = tokens.CrearAccessToken(usuario);
        var refreshToken = TokenService.CrearRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UsuarioId = usuario.Id,
            TokenHash = TokenService.HashToken(refreshToken),
            CreadoEn = DateTimeOffset.UtcNow,
            ExpiraEn = DateTimeOffset.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync(cancellationToken);
        return new SesionResponse(accessToken, refreshToken, expira, new UsuarioResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.Rol is { Activo: true } ? usuario.Rol.Codigo : null));
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record UsuarioResponse(long Id, string Nombre, string Email, string? Rol);
public sealed record SesionResponse(string AccessToken, string RefreshToken, DateTime ExpiraUtc, UsuarioResponse Usuario);
