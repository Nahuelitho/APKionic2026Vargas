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
        .OrderBy(x => x.NombreEmpresa).Select(x => new EmpresaResponse(x.Id, x.NombreEmpresa, x.Activo)).ToListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Crear(EmpresaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NombreEmpresa) || request.NombreEmpresa.Trim().Length > 160)
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "El nombre es obligatorio y admite hasta 160 caracteres." });
        var empresa = new Empresa { NombreEmpresa = request.NombreEmpresa.Trim() };
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync(ct);
        return Created($"/api/empresas/{empresa.Id}", new EmpresaResponse(empresa.Id, empresa.NombreEmpresa, empresa.Activo));
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Actualizar(long id, EmpresaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.NombreEmpresa) || request.NombreEmpresa.Trim().Length > 160)
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "El nombre es obligatorio y admite hasta 160 caracteres." });
        var empresa = await db.Empresas.FindAsync([id], ct);
        if (empresa is null) return NotFound();
        empresa.NombreEmpresa = request.NombreEmpresa.Trim(); empresa.Activo = request.Activo;
        if (!empresa.Activo)
        {
            var sesiones = await db.RefreshTokens.Where(x => x.EmpresaId == id && x.RevocadoEn == null).ToListAsync(ct);
            sesiones.ForEach(x => x.RevocadoEn = DateTimeOffset.UtcNow);
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public sealed record EmpresaRequest(string NombreEmpresa, bool Activo = true);
public sealed record EmpresaResponse(long Id, string NombreEmpresa, bool Activo);
