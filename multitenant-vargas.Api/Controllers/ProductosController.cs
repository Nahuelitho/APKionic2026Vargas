using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var productos = await db.Productos.AsNoTracking()
            .OrderBy(x => x.Nombre)
            .Select(x => new ProductoResponse(
                x.Id,
                x.Nombre,
                x.Descripcion,
                x.Precio,
                x.Stock
            ))
            .ToListAsync(cancellationToken);

        return Ok(new { productos });
    }
}

public sealed record ProductoResponse(
    long Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    bool Stock
);