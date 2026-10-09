using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Controllers;
using Xunit;

namespace Unidad6.Tests;

public sealed class PedidosTests : IClassFixture<BackendFixture>
{
    public PedidosTests(BackendFixture fixture) => Assert.NotNull(fixture);

    [Fact]
    public async Task Crear_CapturaCatalogoCalculaImportesYConservaHistorial()
    {
        using var scope = new BackendScope();
        var primero = await scope.Seed(12.50m, nombre: "Primero");
        var segundo = await scope.Seed(0.75m, nombre: "Segundo");
        var antes = DateTime.UtcNow;
        var result = Assert.IsType<CreatedAtActionResult>(await scope.Pedidos.Crear(
            new PedidoRequest(" Ana ", [new(primero.Id, 3), new(segundo.Id, 4)]), default));
        var pedido = Assert.IsType<PedidoDetalleResponse>(result.Value);
        Assert.Equal(nameof(PedidosController.Obtener), result.ActionName);
        Assert.Equal(pedido.Id, result.RouteValues!["id"]);
        Assert.Equal("Ana", pedido.Cliente);
        Assert.Equal("En preparacion", pedido.Estado);
        Assert.Equal(DateTimeKind.Utc, pedido.Fecha.Kind);
        Assert.InRange(pedido.Fecha, antes, DateTime.UtcNow);
        Assert.Equal(40.50m, pedido.Total);
        Assert.Collection(pedido.Items,
            x => { Assert.Equal("Primero", x.Nombre); Assert.Equal(12.50m, x.PrecioUnitario); Assert.Equal(3, x.Cantidad); Assert.Equal(37.50m, x.Subtotal); },
            x => { Assert.Equal("Segundo", x.Nombre); Assert.Equal(0.75m, x.PrecioUnitario); Assert.Equal(4, x.Cantidad); Assert.Equal(3m, x.Subtotal); });

        primero.Nombre = "Cambiado";
        primero.Precio = 999m;
        await scope.Db.SaveChangesAsync();
        Assert.IsType<NoContentResult>(await scope.Productos.Eliminar(segundo.Id, default));
        scope.Db.ChangeTracker.Clear();
        var historial = Assert.IsType<PedidoDetalleResponse>(Assert.IsType<OkObjectResult>(
            await scope.Pedidos.Obtener(pedido.Id, default)).Value);
        Assert.Equal("Primero", historial.Items[0].Nombre);
        Assert.Equal(12.50m, historial.Items[0].PrecioUnitario);
        Assert.Null(historial.Items[1].ProductoId);
        Assert.Equal("Segundo", historial.Items[1].Nombre);
        Assert.Equal(3m, historial.Items[1].Subtotal);
        Assert.Equal(pedido.Total, historial.Total);
        Assert.True(primero.Stock);
    }

    public static IEnumerable<object[]> SolicitudesInvalidas()
    {
        yield return [new PedidoRequest(" ", [new(1, 1)]), "cliente_invalido"];
        yield return [new PedidoRequest(new string('x', 161), [new(1, 1)]), "cliente_invalido"];
        yield return [new PedidoRequest("Ana", null), "items_invalidos"];
        yield return [new PedidoRequest("Ana", []), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [null]), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [new(0, 1)]), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [new(1, 0)]), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [new(1, -1)]), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [new(1, 10001)]), "items_invalidos"];
        yield return [new PedidoRequest("Ana", Enumerable.Range(1, 101).Select(x => (PedidoItemRequest?)new PedidoItemRequest(x, 1)).ToList()), "items_invalidos"];
        yield return [new PedidoRequest("Ana", [new(1, 1), new(1, 2)]), "producto_duplicado"];
        yield return [new PedidoRequest("Ana", [new(999, 1)]), "producto_no_encontrado"];
    }

    [Theory]
    [MemberData(nameof(SolicitudesInvalidas))]
    public async Task Crear_RechazaSolicitudesInvalidasSinPersistir(PedidoRequest request, string codigo)
    {
        using var scope = new BackendScope();
        BackendScope.Error<BadRequestObjectResult>(await scope.Pedidos.Crear(request, default), 400, codigo);
        Assert.Empty(await scope.Db.Pedidos.ToListAsync());
        Assert.Empty(await scope.Db.PedidoItems.ToListAsync());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10000)]
    public async Task Crear_AceptaLimitesDeCantidad(int cantidad)
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido(cantidad);
        Assert.Equal(cantidad, Assert.Single(pedido.Items).Cantidad);
        Assert.Equal(12.50m * cantidad, pedido.Total);
    }

    [Fact]
    public async Task Crear_SinStockDevuelve409()
    {
        using var scope = new BackendScope();
        var producto = await scope.Seed(stock: false);
        BackendScope.Error<ConflictObjectResult>(await scope.Pedidos.Crear(
            new PedidoRequest("Ana", [new(producto.Id, 1)]), default), 409, "producto_sin_stock");
        Assert.Empty(await scope.Db.Pedidos.ToListAsync());
    }

    [Theory]
    [InlineData("0", 1, false)]
    [InlineData("-1", 1, false)]
    [InlineData("9999999999.99", 2, false)]
    [InlineData("6000000000", 1, true)]
    public async Task Crear_RechazaImporteInvalidoOSumaExcesiva(string precio, int cantidad, bool dosProductos)
    {
        using var scope = new BackendScope();
        var producto = await scope.Seed(decimal.Parse(precio, System.Globalization.CultureInfo.InvariantCulture));
        var items = new List<PedidoItemRequest?> { new(producto.Id, cantidad) };
        if (dosProductos) items.Add(new((await scope.Seed(producto.Precio)).Id, 1));
        BackendScope.Error<BadRequestObjectResult>(await scope.Pedidos.Crear(new("Ana", items), default), 400, "importe_invalido");
        Assert.Empty(await scope.Db.Pedidos.ToListAsync());
        Assert.Empty(await scope.Db.PedidoItems.ToListAsync());
    }

    [Theory]
    [InlineData("En preparacion")]
    [InlineData("Listo")]
    [InlineData("Cancelado")]
    public async Task CambiarEstado_AceptaTransicionesDesdePreparacion(string estado)
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido();
        var actualizado = Assert.IsType<PedidoDetalleResponse>(Assert.IsType<OkObjectResult>(
            await scope.Pedidos.CambiarEstado(pedido.Id, new(estado), default)).Value);
        Assert.Equal(estado, actualizado.Estado);
        using var db = scope.NewContext();
        Assert.Equal(estado, (await db.Pedidos.SingleAsync()).Estado);
    }

    [Theory]
    [InlineData("listo")]
    [InlineData("Listo ")]
    [InlineData("")]
    [InlineData("En Preparacion")]
    public async Task CambiarEstado_RequiereEstadoExacto(string estado)
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido();
        BackendScope.Error<BadRequestObjectResult>(await scope.Pedidos.CambiarEstado(pedido.Id, new(estado), default), 400, "estado_invalido");
        Assert.Equal("En preparacion", (await scope.Db.Pedidos.SingleAsync()).Estado);
    }

    [Theory]
    [InlineData("Listo", "Listo")]
    [InlineData("Listo", "Cancelado")]
    [InlineData("Listo", "En preparacion")]
    [InlineData("Cancelado", "Cancelado")]
    [InlineData("Cancelado", "Listo")]
    [InlineData("Cancelado", "En preparacion")]
    public async Task CambiarEstado_TerminalNoSeModifica(string inicial, string siguiente)
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido();
        Assert.IsType<OkObjectResult>(await scope.Pedidos.CambiarEstado(pedido.Id, new(inicial), default));
        BackendScope.Error<ConflictObjectResult>(await scope.Pedidos.CambiarEstado(pedido.Id, new(siguiente), default), 409, "pedido_terminal");
        using var db = scope.NewContext();
        Assert.Equal(inicial, (await db.Pedidos.SingleAsync()).Estado);
    }

    [Fact]
    public async Task CambiarEstado_ConflictoConcurrenteNoSobrescribeEstado()
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido();
        using var stale = scope.NewContext();
        await stale.Pedidos.Include(x => x.Items).SingleAsync();
        Assert.IsType<OkObjectResult>(await scope.Pedidos.CambiarEstado(pedido.Id, new("Listo"), default));
        BackendScope.Error<ConflictObjectResult>(await BackendScope.Controller(stale).CambiarEstado(
            pedido.Id, new("Cancelado"), default), 409, "pedido_modificado");
        using var fresh = scope.NewContext();
        Assert.Equal("Listo", (await fresh.Pedidos.SingleAsync()).Estado);
    }

    [Fact]
    public async Task RecursosInexistentes_Devuelven404()
    {
        using var scope = new BackendScope();
        BackendScope.Error<NotFoundObjectResult>(await scope.Pedidos.Obtener(999, default), 404, "pedido_no_encontrado");
        BackendScope.Error<NotFoundObjectResult>(await scope.Pedidos.CambiarEstado(999, new("Listo"), default), 404, "pedido_no_encontrado");
        BackendScope.Error<NotFoundObjectResult>(await scope.Pedidos.Comprobante(999, default), 404, "pedido_no_encontrado");
        BackendScope.Error<NotFoundObjectResult>(await scope.Pedidos.Qr(999, default), 404, "pedido_no_encontrado");
    }

    [Theory]
    [InlineData("En preparacion")]
    [InlineData("Cancelado")]
    public async Task Comprobante_NoListoDevuelve409(string estado)
    {
        using var scope = new BackendScope();
        var pedido = await scope.CrearPedido();
        await scope.Pedidos.CambiarEstado(pedido.Id, new(estado), default);
        BackendScope.Error<ConflictObjectResult>(await scope.Pedidos.Comprobante(pedido.Id, default), 409, "comprobante_no_disponible");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Comprobante_ListoGeneraPdfRealConCabeceraYFin(int cantidadItems)
    {
        using var scope = new BackendScope();
        var items = new List<PedidoItemRequest?>();
        for (var i = 0; i < cantidadItems; i++)
            items.Add(new((await scope.Seed(nombre: new string('X', 160))).Id, 2));
        var pedido = Assert.IsType<PedidoDetalleResponse>(Assert.IsType<CreatedAtActionResult>(
            await scope.Pedidos.Crear(new(new string('C', 160), items), default)).Value);
        await scope.Pedidos.CambiarEstado(pedido.Id, new("Listo"), default);
        var file = Assert.IsType<FileContentResult>(await scope.Pedidos.Comprobante(pedido.Id, default));
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal($"PED-{pedido.Id:D4}.pdf", file.FileDownloadName);
        Assert.True(file.FileContents.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(file.FileContents, 0, 5));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(file.FileContents[^32..]));
    }

    [Theory]
    [InlineData("En preparacion", 1L)]
    [InlineData("Listo", 12345L)]
    [InlineData("Cancelado", 42L)]
    public async Task Qr_GeneraPngEnCualquierEstadoSinTruncarId(string estado, long id)
    {
        using var scope = new BackendScope();
        scope.Db.Pedidos.Add(new() { Id = id, EmpresaId = 1, UsuarioId = 1, Cliente = "Ana", Fecha = DateTime.UtcNow, Estado = estado });
        await scope.Db.SaveChangesAsync();
        var file = Assert.IsType<FileContentResult>(await scope.Pedidos.Qr(id, default));
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal($"PED-{id:D4}.png", file.FileDownloadName);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, file.FileContents[..8]);
        Assert.Equal("IHDR", Encoding.ASCII.GetString(file.FileContents, 12, 4));
        Assert.True(file.FileContents.Length > 100);
    }

    [Fact]
    public async Task Crear_FalloDePersistenciaDevuelve500SinGuardarRenglones()
    {
        using var scope = new BackendScope();
        var producto = await scope.Seed();
        scope.Failure.Enabled = true;
        BackendScope.Error<ObjectResult>(await scope.Pedidos.Crear(new("Ana", [new(producto.Id, 1)]), default), 500, "pedido_no_guardado");
        using var db = scope.NewContext();
        Assert.Empty(await db.Pedidos.ToListAsync());
        Assert.Empty(await db.PedidoItems.ToListAsync());
    }
}
