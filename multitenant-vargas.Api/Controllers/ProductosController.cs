using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;
using System.Text.Json.Serialization;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Authorize]
public sealed class ProductosController(AppDbContext db, ProductoFotoService fotos, ILogger<ProductosController> logger) : ControllerBase
{
    [HttpGet("/uploads/productos/{archivo}")]
    public async Task<IActionResult> Foto(string archivo, [FromServices] IWebHostEnvironment environment, CancellationToken ct)
    {
        var mime = Path.GetExtension(archivo).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => null
        };
        if (mime is null || Path.GetFileName(archivo) != archivo ||
            !await AmbitoService.Catalogo(db, User).AnyAsync(x => x.FotoUrl == "/uploads/productos/" + archivo, ct)) return NotFound();
        var path = Path.Combine(environment.WebRootPath, "uploads", "productos", archivo);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(path, mime);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanio = 10,
        CancellationToken cancellationToken = default,
        [FromQuery(Name = "empresa_id")] long? empresaId = null)
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

        var consulta = AmbitoService.Catalogo(db, User).AsNoTracking();
        if (empresaId is not null) consulta = consulta.Where(x => x.EmpresaId == empresaId);

        var total = await consulta.CountAsync(cancellationToken);
        var totalPaginas = (int)Math.Ceiling(total / (double)tamanio);

        var productos = await consulta
            .OrderBy(x => x.Empresa.NombreEmpresa).ThenBy(x => x.Nombre).ThenBy(x => x.Id)
            .Skip((pagina - 1) * tamanio)
            .Take(tamanio)
            .Select(x => new ProductoResponse(
                x.Id,
                x.Nombre,
                x.Descripcion,
                x.Precio,
                x.Stock,
                x.FotoUrl, x.EmpresaId, x.Empresa.NombreEmpresa
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
        var producto = await AmbitoService.Catalogo(db, User).AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductoResponse(
                x.Id,
                x.Nombre,
                x.Descripcion,
                x.Precio,
                x.Stock,
                x.FotoUrl, x.EmpresaId, x.Empresa.NombreEmpresa
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
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> Crear(
        [FromForm] ProductoRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidarProducto(request);

        if (error is not null)
        {
            return BadRequest(error);
        }

        var empresaId = AmbitoService.Global(User) ? request.EmpresaId : AmbitoService.EmpresaId(User);
        if (empresaId is null || (request.EmpresaId is not null && request.EmpresaId != empresaId))
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "Seleccione una empresa de su ambito." });
        var empresa = await db.Empresas.FirstOrDefaultAsync(x => x.Id == empresaId && x.Activo, cancellationToken);
        if (empresa is null) return BadRequest(new { codigo = "empresa_invalida", mensaje = "La empresa no esta disponible." });
        var producto = new Producto
        {
            Nombre = request.Nombre.Trim(),
            EmpresaId = empresa.Id, Empresa = empresa,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
                ? null
                : request.Descripcion.Trim(),
            Precio = request.Precio,
            Stock = request.Stock
        };

        var foto = await fotos.GuardarAsync(request.Foto, cancellationToken);
        if (foto.Error is not null) return BadRequest(new { codigo = "foto_invalida", mensaje = foto.Error });
        producto.FotoUrl = foto.Url;
        db.Productos.Add(producto);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (Exception ex)
        {
            fotos.Borrar(foto.Url);
            if (ex is OperationCanceledException) throw;
            logger.LogError(ex, "No se pudo guardar el producto.");
            return StatusCode(500, new { codigo = "producto_no_guardado", mensaje = "No se pudo guardar el producto." });
        }

        return CreatedAtAction(
            nameof(ObtenerPorId),
            new { id = producto.Id },
            new ProductoResponse(
                producto.Id,
                producto.Nombre,
                producto.Descripcion,
                producto.Precio,
                producto.Stock,
                producto.FotoUrl, producto.EmpresaId, producto.Empresa.NombreEmpresa
            )
        );
    }

    [HttpPut("{id:long}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> Actualizar(
        long id,
        [FromForm] ProductoRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidarProducto(request);

        if (error is not null)
        {
            return BadRequest(error);
        }

        var producto = await AmbitoService.Catalogo(db, User).Include(x => x.Empresa)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (producto is null)
        {
            return NotFound(new
            {
                codigo = "producto_no_encontrado",
                mensaje = "No se encontro el producto."
            });
        }

        if (request.EmpresaId is not null && request.EmpresaId != producto.EmpresaId)
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "No se puede cambiar la empresa de un producto." });
        var foto = await fotos.GuardarAsync(request.Foto, cancellationToken);
        if (foto.Error is not null) return BadRequest(new { codigo = "foto_invalida", mensaje = foto.Error });
        var fotoAnterior = producto.FotoUrl;
        if (foto.Url is not null) producto.FotoUrl = foto.Url;
        producto.Nombre = request.Nombre.Trim();
        producto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
            ? null
            : request.Descripcion.Trim();
        producto.Precio = request.Precio;
        producto.Stock = request.Stock;

        try { await db.SaveChangesAsync(cancellationToken); }
        catch (Exception ex)
        {
            fotos.Borrar(foto.Url);
            if (ex is OperationCanceledException) throw;
            logger.LogError(ex, "No se pudo actualizar el producto {ProductoId}.", id);
            return StatusCode(500, new { codigo = "producto_no_guardado", mensaje = "No se pudo guardar el producto." });
        }
        if (foto.Url is not null) fotos.Borrar(fotoAnterior);

        return Ok(new ProductoResponse(
            producto.Id,
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.FotoUrl, producto.EmpresaId, producto.Empresa.NombreEmpresa
        ));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> Eliminar(
        long id,
        CancellationToken cancellationToken)
    {
        var producto = await AmbitoService.Catalogo(db, User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (producto is null)
        {
            return NotFound(new
            {
                codigo = "producto_no_encontrado",
                mensaje = "No se encontro el producto."
            });
        }

        // SET NULL on the composite FK would also null EmpresaId. Keep the tenant and snapshots.
        var items = await db.PedidoItems.Where(x => x.ProductoId == id && x.EmpresaId == producto.EmpresaId).ToListAsync(cancellationToken);
        items.ForEach(x => x.ProductoId = null);
        db.Productos.Remove(producto);
        await db.SaveChangesAsync(cancellationToken);
        fotos.Borrar(producto.FotoUrl);

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

        if (request.Precio <= 0 || request.Precio > 9999999999.99m || decimal.Round(request.Precio, 2) != request.Precio)
        {
            return new
            {
                codigo = "precio_invalido",
                mensaje = "El precio debe ser mayor a 0, hasta 9999999999.99 y tener como maximo dos decimales."
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
    bool Stock,
    [property: JsonPropertyName("fotoUrl")] string? FotoUrl,
    long EmpresaId,
    string NombreEmpresa
);
public sealed record ProductoRequest(
    string Nombre,
    string? Descripcion,
    decimal Precio,
    bool Stock,
    IFormFile? Foto = null,
    long? EmpresaId = null
);
