using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize(Roles = "administrador,vendedor")]
public sealed class ProductosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanio = 10,
        CancellationToken cancellationToken = default)
    {
        if (pagina < 1)
        {
            return BadRequest(new
            {
                codigo = "pagina_invalida",
                mensaje = "La pagina debe ser mayor o igual a 1."
            });
        }

        if (tamanio < 1 || tamanio > 50)
        {
            return BadRequest(new
            {
                codigo = "tamanio_invalido",
                mensaje = "El tamanio debe estar entre 1 y 50."
            });
        }

        var consulta = db.Productos.AsNoTracking();

        var total = await consulta.CountAsync(cancellationToken);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanio);

        var productos = await consulta
            .OrderBy(x => x.Nombre)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .Select(x => new ProductoResponse(
                x.Id,
                x.Nombre,
                x.Descripcion,
                x.Precio,
                x.Stock
            ))
            .ToListAsync(cancellationToken);

        return Ok(new ProductosPaginadosResponse(
            productos,
            pagina,
            tamanio,
            total,
            totalPaginas
        ));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> ObtenerPorId(
        long id,
        CancellationToken cancellationToken)
    {
        var producto = await db.Productos.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductoResponse(
                x.Id,
                x.Nombre,
                x.Descripcion,
                x.Precio,
                x.Stock
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (producto is null)
        {
            return NotFound(new
            {
                codigo = "producto_no_encontrado",
                mensaje = "No se encontro el producto."
            });
        }

        return Ok(producto);
    }

    [HttpPost]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Crear(
        [FromBody] ProductoRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidarProducto(request);

        if (error is not null)
        {
            return BadRequest(error);
        }

        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
                ? null
                : request.Descripcion.Trim(),
            Precio = request.Precio,
            Stock = request.Stock
        };

        db.Productos.Add(producto);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = producto.Id },
            new ProductoResponse(
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.Precio,
                producto.Stock
            )
        );
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Actualizar(
        long id,
        [FromBody] ProductoRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidarProducto(request);

        if (error is not null)
        {
            return BadRequest(error);
        }

        var producto = await db.Productos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (producto is null)
        {
            return NotFound(new
            {
                codigo = "producto_no_encontrado",
                mensaje = "No se encontro el producto."
            });
        }

        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
            ? null
            : request.Descripcion.Trim();
        producto.Precio = request.Precio;
        producto.Stock = request.Stock;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new ProductoResponse(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock
        ));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "administrador")]
    public async Task<IActionResult> Eliminar(
        long id,
        CancellationToken cancellationToken)
    {
        var producto = await db.Productos
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (producto is null)
        {
            return NotFound(new
            {
                codigo = "producto_no_encontrado",
                mensaje = "No se encontro el producto."
            });
        }

        db.Productos.Remove(producto);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static object? ValidarProducto(ProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return new
            {
                codigo = "nombre_obligatorio",
                mensaje = "El nombre del producto es obligatorio."
            };
        }

        if (request.Nombre.Trim().Length > 160)
        {
            return new
            {
                codigo = "nombre_muy_largo",
                mensaje = "El nombre no puede superar los 160 caracteres."
            };
        }

        if (!string.IsNullOrWhiteSpace(request.Descripcion) &&
            request.Descripcion.Trim().Length > 1000)
        {
            return new
            {
                codigo = "descripcion_muy_larga",
                mensaje = "La descripcion no puede superar los 1000 caracteres."
            };
        }

        if (request.Precio <= 0)
        {
            return new
            {
                codigo = "precio_invalido",
                mensaje = "El precio debe ser mayor a 0."
            };
        }

        return null;
    }
}

public sealed record ProductosPaginadosResponse(
    IReadOnlyList<ProductoResponse> Productos,
    int Pagina,
    int Tamanio,
    int Total,
    int TotalPaginas
);

public sealed record ProductoResponse(
    long Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    bool Stock
);
public sealed record ProductoRequest(
    string Nombre,
    string? Descripcion,
    decimal Precio,
    bool Stock
);
