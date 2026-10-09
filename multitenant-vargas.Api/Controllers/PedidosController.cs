using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace multitenant_vargas.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
[Authorize]
public sealed class PedidosController(AppDbContext db, ILogger<PedidosController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var pedidos = await AmbitoService.Pedidos(db, User).AsNoTracking().OrderByDescending(x => x.Id)
            .Select(x => new PedidoResumenResponse(x.Id, x.Cliente,
                DateTime.SpecifyKind(x.Fecha, DateTimeKind.Utc), x.Estado, x.Total, x.EmpresaId, x.UsuarioId, x.Empresa.NombreEmpresa)).ToListAsync(ct);
        return Ok(new { pedidos });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Obtener(long id, CancellationToken ct)
    {
        var pedido = await AmbitoService.Pedidos(db, User).AsNoTracking().Include(x => x.Empresa).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
        return pedido is null ? NoEncontrado() : Ok(Respuesta(pedido));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Crear([FromBody] PedidoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Cliente) || request.Cliente.Trim().Length > 160)
            return BadRequest(new { codigo = "cliente_invalido", mensaje = "El cliente es obligatorio y admite hasta 160 caracteres." });
        if (request.Items is null || request.Items.Count is < 1 or > 100 ||
            request.Items.Any(x => x is null || x.ProductoId <= 0 || x.Cantidad is < 1 or > 10000))
            return BadRequest(new { codigo = "items_invalidos", mensaje = "Envie entre 1 y 100 items con productoId positivo y cantidad entera entre 1 y 10000." });
        if (request.Items.Select(x => x!.ProductoId).Distinct().Count() != request.Items.Count)
            return BadRequest(new { codigo = "producto_duplicado", mensaje = "Cada producto debe aparecer una sola vez en el pedido." });

        var ids = request.Items.Select(x => x!.ProductoId).ToArray();
        var productos = await AmbitoService.Catalogo(db, User).Include(x => x.Empresa).Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (productos.Count != ids.Length)
            return BadRequest(new { codigo = "producto_no_encontrado", mensaje = "Uno o mas productos no existen." });
        var empresaId = productos.Values.First().EmpresaId;
        if (productos.Values.Any(x => x.EmpresaId != empresaId) || (request.EmpresaId is not null && request.EmpresaId != empresaId))
            return BadRequest(new { codigo = "empresa_invalida", mensaje = "Todos los productos deben pertenecer a una unica empresa." });
        if (productos.Values.Any(x => !x.Stock))
            return Conflict(new { codigo = "producto_sin_stock", mensaje = "Uno o mas productos no estan disponibles." });

        var pedido = new Pedido { Cliente = request.Cliente.Trim(), Fecha = DateTime.UtcNow,
            EmpresaId = empresaId, Empresa = productos.Values.First().Empresa, UsuarioId = AmbitoService.UsuarioId(User) };
        foreach (var item in request.Items)
        {
            var producto = productos[item!.ProductoId];
            var subtotal = producto.Precio * item.Cantidad;
            if (producto.Precio <= 0 || subtotal > 9999999999.99m)
                return BadRequest(new { codigo = "importe_invalido", mensaje = "Los importes deben ser positivos y no superar 9999999999.99." });
            pedido.Items.Add(new PedidoItem
            {
                ProductoId = producto.Id, EmpresaId = empresaId, Nombre = producto.Nombre,
                PrecioUnitario = producto.Precio, Cantidad = item.Cantidad, Subtotal = subtotal
            });
            pedido.Total += subtotal;
        }
        if (pedido.Total > 9999999999.99m)
            return BadRequest(new { codigo = "importe_invalido", mensaje = "El total no puede superar 9999999999.99." });

        db.Pedidos.Add(pedido);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "No se pudo guardar el pedido.");
            return StatusCode(500, new { codigo = "pedido_no_guardado", mensaje = "No se pudo guardar el pedido. Vuelva a intentarlo." });
        }
        return CreatedAtAction(nameof(Obtener), new { id = pedido.Id }, Respuesta(pedido));
    }

    [HttpPut("{id:long}/estado")]
    [Authorize(Roles = "superadmin,administrador,vendedor,caja")]
    public async Task<IActionResult> CambiarEstado(long id, [FromBody] PedidoEstadoRequest request, CancellationToken ct)
    {
        if (request.Estado is not ("En preparacion" or "Listo" or "Cancelado"))
            return BadRequest(new { codigo = "estado_invalido", mensaje = "Los estados exactos son: En preparacion, Listo, Cancelado." });
        var pedido = await AmbitoService.Pedidos(db, User).Include(x => x.Empresa).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (pedido is null) return NoEncontrado();
        if (pedido.Estado != "En preparacion")
            return Conflict(new { codigo = "pedido_terminal", mensaje = "Un pedido Listo o Cancelado no puede modificarse." });

        pedido.Estado = request.Estado;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { codigo = "pedido_modificado", mensaje = "El pedido cambio de estado. Actualice el detalle antes de continuar." });
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "No se pudo actualizar el pedido {PedidoId}.", id);
            return StatusCode(500, new { codigo = "pedido_no_guardado", mensaje = "No se pudo guardar el estado del pedido." });
        }
        return Ok(Respuesta(pedido));
    }

    [HttpGet("{id:long}/qr")]
    public async Task<IActionResult> Qr(long id, CancellationToken ct)
    {
        if (!await AmbitoService.Pedidos(db, User).AsNoTracking().AnyAsync(x => x.Id == id, ct)) return NoEncontrado();
        return File(GenerarQr(id), "image/png", $"PED-{id:D4}.png");
    }

    [HttpGet("{id:long}/comprobante")]
    [Authorize(Roles = "superadmin,administrador,vendedor")]
    public async Task<IActionResult> Comprobante(long id, CancellationToken ct)
    {
        var pedido = await AmbitoService.Pedidos(db, User).AsNoTracking().Include(x => x.Empresa).Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (pedido is null) return NoEncontrado();
        if (pedido.Estado != "Listo")
            return Conflict(new { codigo = "comprobante_no_disponible", mensaje = "El comprobante solo esta disponible para pedidos Listo." });

        var qr = GenerarQr(id);
        var cultura = CultureInfo.GetCultureInfo("es-AR");
        var pdf = Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(35);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Column(column =>
            {
                column.Item().Text($"Comprobante PED-{pedido.Id:D4}").FontSize(22).Bold();
                column.Item().Text(pedido.Empresa.NombreEmpresa);
                column.Item().Text($"Cliente: {pedido.Cliente}");
                column.Item().Text($"Fecha (UTC): {pedido.Fecha:yyyy-MM-dd HH:mm} | Estado: {pedido.Estado}");
            });
            page.Content().PaddingVertical(20).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4); columns.RelativeColumn();
                        columns.RelativeColumn(2); columns.RelativeColumn(2);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Producto").Bold(); header.Cell().Text("Cant.").Bold();
                        header.Cell().AlignRight().Text("Precio").Bold(); header.Cell().AlignRight().Text("Subtotal").Bold();
                    });
                    foreach (var item in pedido.Items.OrderBy(x => x.Id))
                    {
                        table.Cell().PaddingVertical(5).Text(item.Nombre);
                        table.Cell().PaddingVertical(5).Text(item.Cantidad.ToString(cultura));
                        table.Cell().PaddingVertical(5).AlignRight().Text(item.PrecioUnitario.ToString("C2", cultura));
                        table.Cell().PaddingVertical(5).AlignRight().Text(item.Subtotal.ToString("C2", cultura));
                    }
                });
                column.Item().PaddingTop(15).AlignRight().Text($"Total: {pedido.Total.ToString("C2", cultura)}").FontSize(16).Bold();
            });
            page.Footer().Column(column =>
            {
                column.Item().AlignCenter().Width(80).Image(qr);
                column.Item().AlignCenter().Text($"PED-{pedido.Id:D4}");
                column.Item().PaddingTop(5).AlignCenter().Text(text =>
                {
                    text.Span("Pagina "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages();
                });
            });
        })).GeneratePdf();
        return File(pdf, "application/pdf", $"PED-{id:D4}.pdf");
    }

    private NotFoundObjectResult NoEncontrado() => NotFound(new
    {
        codigo = "pedido_no_encontrado", mensaje = "No se encontro el pedido."
    });

    private static byte[] GenerarQr(long id)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode($"PED-{id:D4}", QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(10);
    }

    private static PedidoDetalleResponse Respuesta(Pedido pedido) => new(
        pedido.Id, pedido.Cliente, DateTime.SpecifyKind(pedido.Fecha, DateTimeKind.Utc), pedido.Estado, pedido.Total,
        pedido.Items.OrderBy(x => x.Id).Select(x => new PedidoItemResponse(
            x.Id, x.ProductoId, x.Nombre, x.PrecioUnitario, x.Cantidad, x.Subtotal)).ToArray(), pedido.EmpresaId, pedido.UsuarioId, pedido.Empresa.NombreEmpresa);
}

public sealed record PedidoRequest(string Cliente, List<PedidoItemRequest?>? Items, long? EmpresaId = null);
public sealed record PedidoItemRequest([property: JsonPropertyName("productoId")] long ProductoId, int Cantidad);
public sealed record PedidoEstadoRequest(string Estado);
public sealed record PedidoResumenResponse(long Id, string Cliente, DateTime Fecha, string Estado, decimal Total, long EmpresaId, long UsuarioId, string NombreEmpresa);
public sealed record PedidoDetalleResponse(long Id, string Cliente, DateTime Fecha, string Estado, decimal Total, IReadOnlyList<PedidoItemResponse> Items, long EmpresaId, long UsuarioId, string NombreEmpresa);
public sealed record PedidoItemResponse(long Id, [property: JsonPropertyName("productoId")] long? ProductoId,
    string Nombre, [property: JsonPropertyName("precioUnitario")] decimal PrecioUnitario, int Cantidad, decimal Subtotal);
