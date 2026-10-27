using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/empresas")]
public sealed class EmpresasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await db.Empresas.AsNoTracking()
        .Where(x => (x.Activo || AmbitoService.Global(User)) &&
            (!AmbitoService.Gestion(User) || AmbitoService.Global(User) || x.Id == AmbitoService.EmpresaId(User)))
        .OrderBy(x => x.NombreEmpresa)
        .Select(x => new EmpresaResponse(x.Id, x.NombreEmpresa, x.Activo, x.DireccionLinea, x.Referencia, x.Latitud, x.Longitud))
        .ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Crear(EmpresaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NombreEmpresa) || request.NombreEmpresa.Trim().Length > 160)
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "El nombre es obligatorio y admite hasta 160 caracteres." });
        var empresa = new Empresa { NombreEmpresa = request.NombreEmpresa.Trim() };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync(ct);
        return Created($"/api/empresas/{empresa.Id}", new EmpresaResponse(empresa.Id, empresa.NombreEmpresa, empresa.Activo, empresa.DireccionLinea, empresa.Referencia, empresa.Latitud, empresa.Longitud));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> Actualizar(long id, EmpresaRequest request, CancellationToken ct)
    {
        // Cada uno edita lo suyo: el superadmin cualquiera, el resto solo su empresa.
        if (!AmbitoService.Global(User) && AmbitoService.EmpresaId(User) != id)
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.NombreEmpresa) || request.NombreEmpresa.Trim().Length > 160)
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "El nombre es obligatorio y admite hasta 160 caracteres." });

        var empresa = await db.Empresas.FindAsync([id], ct);
        if (empresa is null) return NotFound();

        empresa.NombreEmpresa = request.NombreEmpresa.Trim();
        empresa.DireccionLinea = string.IsNullOrWhiteSpace(request.DireccionLinea) ? null : request.DireccionLinea.Trim();
        empresa.Referencia = string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim();
        empresa.Latitud = request.Latitud;
        empresa.Longitud = request.Longitud;

        // El vendedor edita nombre y ubicación, pero activar/desactivar es de admin o superadmin.
        if (!User.IsInRole("vendedor"))
        {
            empresa.Activo = request.Activo;
            if (!empresa.Activo)
            {
                var sesiones = await db.RefreshTokens.Where(x => x.EmpresaId == id && x.RevocadoEn == null).ToListAsync(ct);
                sesiones.ForEach(x => x.RevocadoEn = DateTimeOffset.UtcNow);
            }
        }

        await db.SaveChangesAsync(ct);
        return Ok(new EmpresaResponse(empresa.Id, empresa.NombreEmpresa, empresa.Activo, empresa.DireccionLinea, empresa.Referencia, empresa.Latitud, empresa.Longitud));
    }

    [HttpPut("{id:long}/ubicacion")]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> GuardarUbicacion(long id, EmpresaUbicacionRequest request, CancellationToken ct)
    {
        if (!AmbitoService.Global(User) && AmbitoService.EmpresaId(User) != id)
            return Forbid();

        var empresa = await db.Empresas.FindAsync([id], ct);
        if (empresa is null) return NotFound();

        empresa.DireccionLinea = string.IsNullOrWhiteSpace(request.DireccionLinea) ? null : request.DireccionLinea.Trim();
        empresa.Referencia = string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim();
        empresa.Latitud = request.Latitud;
        empresa.Longitud = request.Longitud;

        await db.SaveChangesAsync(ct);
        return Ok(new EmpresaResponse(empresa.Id, empresa.NombreEmpresa, empresa.Activo, empresa.DireccionLinea, empresa.Referencia, empresa.Latitud, empresa.Longitud));
    }
}

public sealed record EmpresaRequest(string NombreEmpresa, bool Activo = true, string? DireccionLinea = null, string? Referencia = null, decimal? Latitud = null, decimal? Longitud = null);
public sealed record EmpresaUbicacionRequest(string? DireccionLinea, string? Referencia, decimal? Latitud, decimal? Longitud);
public sealed record EmpresaResponse(long Id, string NombreEmpresa, bool Activo, string? DireccionLinea = null, string? Referencia = null, decimal? Latitud = null, decimal? Longitud = null);