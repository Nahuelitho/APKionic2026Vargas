using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Controllers;
using multitenant_vargas.Api.Services;
using Xunit;

namespace Unidad6.Tests;

public sealed class FotosTests
{
    private static byte[] Firma(string mime) => mime switch
    {
        "image/jpeg" => [255, 216, 255],
        "image/png" => [137, 80, 78, 71, 13, 10, 26, 10],
        "image/webp" => "RIFF1234WEBP"u8.ToArray(),
        _ => [1, 2, 3]
    };

    private static FormFile Foto(byte[] bytes, string mime = "image/png", long? length = null) =>
        new(new MemoryStream(bytes), 0, length ?? bytes.Length, "foto", "../../nombre-no-confiable.exe")
        { Headers = new HeaderDictionary(), ContentType = mime };

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("image/png", ".png")]
    [InlineData("image/webp", ".webp")]
    public async Task Guardar_ValidaFirmaUsaGuidYConservaBytes(string mime, string extension)
    {
        using var scope = new BackendScope();
        var bytes = Firma(mime).Concat(Enumerable.Range(0, 250).Select(x => (byte)x)).ToArray();
        var (url, error) = await scope.Fotos.GuardarAsync(Foto(bytes, mime), default);
        Assert.Null(error);
        Assert.NotNull(url);
        Assert.StartsWith("/uploads/productos/", url);
        Assert.Equal(extension, Path.GetExtension(url));
        Assert.True(Guid.TryParseExact(Path.GetFileNameWithoutExtension(url), "N", out _));
        Assert.Equal(bytes, await File.ReadAllBytesAsync(scope.FotoPath(url)));
        var segunda = await scope.Fotos.GuardarAsync(Foto(bytes, mime), default);
        Assert.NotEqual(url, segunda.Url);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("text/plain")]
    public async Task Guardar_RechazaFirmaOMimeInvalidosSinArchivo(string mime)
    {
        using var scope = new BackendScope();
        var result = await scope.Fotos.GuardarAsync(Foto([1, 2, 3], mime), default);
        Assert.Null(result.Url);
        Assert.NotNull(result.Error);
        Assert.Empty(Directory.GetFiles(scope.Root, "*", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(5242881L)]
    public async Task Guardar_RechazaLongitudFueraDeLimites(long length)
    {
        using var scope = new BackendScope();
        var result = await scope.Fotos.GuardarAsync(Foto(Firma("image/png"), length: length), default);
        Assert.Null(result.Url);
        Assert.NotNull(result.Error);
        Assert.Empty(Directory.GetFiles(scope.Root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Guardar_AceptaExactamenteCincoMiB()
    {
        using var scope = new BackendScope();
        var bytes = new byte[ProductoFotoService.MaxBytes];
        Firma("image/png").CopyTo(bytes, 0);
        var result = await scope.Fotos.GuardarAsync(Foto(bytes), default);
        Assert.Null(result.Error);
        Assert.NotNull(result.Url);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(scope.FotoPath(result.Url)));
    }

    [Fact]
    public async Task Guardar_SinFotoNoCreaArchivo()
    {
        using var scope = new BackendScope();
        Assert.Equal((null, null), await scope.Fotos.GuardarAsync(null, default));
        Assert.Empty(Directory.GetFiles(scope.Root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task Actualizar_SinFotoConservaPathReemplazarBorraAnteriorYEliminarLimpia()
    {
        using var scope = new BackendScope();
        var request = new ProductoRequest(" Producto ", null, 12.50m, true, Foto(Firma("image/png")));
        var creado = Assert.IsType<ProductoResponse>(Assert.IsType<CreatedAtActionResult>(
            await scope.Productos.Crear(request, default)).Value);
        Assert.NotNull(creado.FotoUrl);
        var path = scope.FotoPath(creado.FotoUrl);
        Assert.True(File.Exists(path));
        var sinFoto = Assert.IsType<ProductoResponse>(Assert.IsType<OkObjectResult>(
            await scope.Productos.Actualizar(creado.Id, request with { Foto = null, Nombre = "Editado" }, default)).Value);
        Assert.Equal(creado.FotoUrl, sinFoto.FotoUrl);
        Assert.True(File.Exists(path));
        var reemplazo = Assert.IsType<ProductoResponse>(Assert.IsType<OkObjectResult>(
            await scope.Productos.Actualizar(creado.Id, request with { Foto = Foto(Firma("image/jpeg"), "image/jpeg") }, default)).Value);
        Assert.NotNull(reemplazo.FotoUrl);
        Assert.NotEqual(creado.FotoUrl, reemplazo.FotoUrl);
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(scope.FotoPath(reemplazo.FotoUrl)));
        Assert.IsType<NoContentResult>(await scope.Productos.Eliminar(creado.Id, default));
        Assert.False(File.Exists(scope.FotoPath(reemplazo.FotoUrl)));
        Assert.Empty(await scope.Db.Productos.ToListAsync());
    }

    [Fact]
    public async Task Actualizar_FotoInvalidaConservaPathYDatos()
    {
        using var scope = new BackendScope();
        var request = new ProductoRequest("Original", null, 12m, true, Foto(Firma("image/png")));
        var producto = Assert.IsType<ProductoResponse>(Assert.IsType<CreatedAtActionResult>(
            await scope.Productos.Crear(request, default)).Value);
        BackendScope.Error<BadRequestObjectResult>(await scope.Productos.Actualizar(producto.Id,
            request with { Nombre = "No guardar", Foto = Foto([1, 2, 3]) }, default), 400, "foto_invalida");
        using var db = scope.NewContext();
        var persisted = await db.Productos.SingleAsync();
        Assert.Equal("Original", persisted.Nombre);
        Assert.Equal(producto.FotoUrl, persisted.FotoUrl);
        Assert.True(File.Exists(scope.FotoPath(producto.FotoUrl!)));
    }

    [Fact]
    public async Task Actualizar_FalloAlGuardarConservaAnteriorYLimpiaNueva()
    {
        using var scope = new BackendScope();
        var request = new ProductoRequest("Original", null, 12m, true, Foto(Firma("image/png")));
        var producto = Assert.IsType<ProductoResponse>(Assert.IsType<CreatedAtActionResult>(
            await scope.Productos.Crear(request, default)).Value);
        scope.Failure.Enabled = true;
        BackendScope.Error<ObjectResult>(await scope.Productos.Actualizar(producto.Id,
            request with { Nombre = "No guardar", Foto = Foto(Firma("image/jpeg"), "image/jpeg") }, default), 500, "producto_no_guardado");
        Assert.Equal(scope.FotoPath(producto.FotoUrl!), Assert.Single(Directory.GetFiles(scope.Root, "*", SearchOption.AllDirectories)));
        using var db = scope.NewContext();
        var persisted = await db.Productos.SingleAsync();
        Assert.Equal(producto.FotoUrl, persisted.FotoUrl);
        Assert.Equal("Original", persisted.Nombre);
    }

    [Fact]
    public async Task Crear_FalloAlGuardarLimpiaFotoNueva()
    {
        using var scope = new BackendScope();
        scope.Failure.Enabled = true;
        BackendScope.Error<ObjectResult>(await scope.Productos.Crear(
            new("Producto", null, 12m, true, Foto(Firma("image/png"))), default), 500, "producto_no_guardado");
        Assert.Empty(Directory.GetFiles(scope.Root, "*", SearchOption.AllDirectories));
        using var db = scope.NewContext();
        Assert.Empty(await db.Productos.ToListAsync());
    }
}
