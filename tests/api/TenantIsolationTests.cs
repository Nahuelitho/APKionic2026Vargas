using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using multitenant_vargas.Api.Controllers;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;
using Xunit;

namespace Unidad6.Tests;

public sealed class TenantIsolationTests
{
    [Fact]
    public async Task Fotos_ExigenSesionYAislanLaEmpresaSinCambiarFotoUrl()
    {
        using var app = new TenantApplication(); using var admin = app.CreateClient(); await app.Seed();
        await Login(admin, "admin@vargas.com", "Admin123!");
        using var form = Form();
        var bytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        var foto = new ByteArrayContent(bytes); foto.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(foto, "foto", "foto.png");
        var creado = await admin.PostAsync("/api/productos", form);
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);
        var producto = await creado.Content.ReadFromJsonAsync<JsonElement>();
        var url = producto.GetProperty("fotoUrl").GetString()!;
        Assert.StartsWith("/uploads/productos/", url);
        using var anonimo = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(url)).StatusCode);
        using var otro = app.CreateClient(); await Login(otro, "admin@segunda.com", "AdminSegunda123!");
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync(url)).StatusCode);
        using var comun = app.CreateClient(); await Login(comun, "sinrol@vargas.com", "SinRol123!");
        var imagen = await comun.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, imagen.StatusCode);
        Assert.Equal(bytes, await imagen.Content.ReadAsByteArrayAsync());
        Assert.Equal("image/png", imagen.Content.Headers.ContentType!.MediaType);
        Assert.True(imagen.Headers.CacheControl!.NoStore);
        await Login(otro, "global@test.com", "Global123!");
        Assert.Equal(HttpStatusCode.OK, (await otro.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/productos/{producto.GetProperty("id").GetInt64()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await comun.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Comun_CatalogoGlobalYPedidosPropiosEnTodasLasEmpresas()
    {
        using var app = new TenantApplication();
        using var client = app.CreateClient();
        await app.Seed();
        await Login(client, "sinrol@vargas.com", "SinRol123!");
        var empresas = await client.GetFromJsonAsync<JsonElement>("/api/empresas");
        Assert.Equal(2, empresas.GetArrayLength());
        var catalogo = await client.GetFromJsonAsync<JsonElement>("/api/productos?tamanio=50");
        Assert.Equal(2, catalogo.GetProperty("total").GetInt32());
        var propios = await client.GetFromJsonAsync<JsonElement>("/api/pedidos");
        Assert.Equal(new long[] { 2001, 1001 }, propios.GetProperty("pedidos").EnumerateArray().Select(x => x.GetProperty("id").GetInt64()));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/pedidos/1001")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/pedidos/2001/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/pedidos/1002")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/pedidos/1002/qr")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/pedidos/2001/comprobante")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PutAsJsonAsync("/api/pedidos/1001/estado", new { estado = "Listo" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/usuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/productos", Form())).StatusCode);
        var mixed = await client.PostAsJsonAsync("/api/pedidos", new { cliente = "Ana", items = new[] { new { productoId = 101, cantidad = 1 }, new { productoId = 201, cantidad = 1 } } });
        Assert.Equal(HttpStatusCode.BadRequest, mixed.StatusCode);
        var mismatch = await client.PostAsJsonAsync("/api/pedidos", new { cliente = "Ana", empresa_id = 1, items = new[] { new { productoId = 201, cantidad = 1 } } });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        var created = await client.PostAsJsonAsync("/api/pedidos", new { cliente = "Ana", empresa_id = 2, usuario_id = 7, items = new[] { new { productoId = 201, cantidad = 2 } } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var pedido = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, pedido.GetProperty("empresa_id").GetInt64());
        Assert.Equal(3, pedido.GetProperty("usuario_id").GetInt64());
    }

    [Theory]
    [InlineData("admin@vargas.com", "Admin123!", 1, 201, 2001)]
    [InlineData("vendedor@vargas.com", "Vendedor123!", 1, 201, 2001)]
    [InlineData("admin@segunda.com", "AdminSegunda123!", 2, 101, 1001)]
    [InlineData("vendedor@segunda.com", "VendedorSegunda123!", 2, 101, 1001)]
    public async Task Membresias_AislanTodosLosRecursos(string email, string password, long empresa, long productoAjeno, long pedidoAjeno)
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        await Login(client, email, password);
        var catalogo = await client.GetFromJsonAsync<JsonElement>("/api/productos?tamanio=50");
        Assert.All(catalogo.GetProperty("productos").EnumerateArray(), p => Assert.Equal(empresa, p.GetProperty("empresa_id").GetInt64()));
        var pedidos = await client.GetFromJsonAsync<JsonElement>("/api/pedidos");
        Assert.All(pedidos.GetProperty("pedidos").EnumerateArray(), p => Assert.Equal(empresa, p.GetProperty("empresa_id").GetInt64()));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/productos/{productoAjeno}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/productos/{productoAjeno}", Form())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/productos/{productoAjeno}")).StatusCode);
        foreach (var suffix in new[] { "", "/qr", "/comprobante" })
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/pedidos/{pedidoAjeno}{suffix}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/pedidos/{pedidoAjeno}/estado", new { estado = "Listo" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/pedidos", new { cliente = "Ana", items = new[] { new { productoId = productoAjeno, cantidad = 1 } } })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/productos", Form(empresa == 1 ? 2 : 1))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/productos", Form())).StatusCode);
        var propio = empresa == 1 ? 1001 : 2001;
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/pedidos/{propio}/comprobante")).StatusCode);
    }

    [Fact]
    public async Task Administrador_NoGestionaUsuariosAjenosNiEscalaAGlobal()
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        await Login(client, "admin@vargas.com", "Admin123!");
        var usuarios = await client.GetFromJsonAsync<JsonElement>("/api/usuarios");
        Assert.Equal(new long[] { 1, 2 }, usuarios.EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).Order());
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/usuarios/5/rol", new { rol_id = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/usuarios/3/rol", new { rol_id = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/usuarios/2/rol", new { rol_id = 4 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/usuarios/2/rol", new { rol_id = 1, empresa_id = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Global", email = "nuevo@test.com", password = "Password123!", rol_id = 4 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/empresas", new { nombre_empresa = "No" })).StatusCode);
        await Login(client, "vendedor@vargas.com", "Vendedor123!");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Superadmin_GestionGlobalYCreacionDeComunSinMembresia()
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        await Login(client, "global@test.com", "Global123!");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/productos/201")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/pedidos/2001/comprobante")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/pedidos/1002/estado", new { estado = "Listo" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/empresas", new { nombre_empresa = "Tercera" })).StatusCode);
        var comun = await client.PostAsJsonAsync("/api/usuarios", new { nombre = "Comun", email = "nuevo@test.com", password = "Password123!" });
        Assert.Equal(HttpStatusCode.Created, comun.StatusCode);
        var id = (await comun.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt64();
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/usuarios/{id}/rol", new { rol_id = 2, empresa_id = 2 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/usuarios/{id}/rol", new { rol_id = 1, empresa_id = 1 })).StatusCode);
        var sesion = await Login(client, "nuevo@test.com", "Password123!", 2);
        Assert.Equal(2, sesion.GetProperty("usuario").GetProperty("empresa_id").GetInt64());
    }

    [Theory]
    [InlineData("membresia")]
    [InlineData("usuario")]
    [InlineData("empresa")]
    [InlineData("rol")]
    public async Task JwtYRefresh_RevalidanElAmbitoActual(string cambio)
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        var sesion = await Login(client, "vendedor@vargas.com", "Vendedor123!");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (cambio == "membresia") (await db.UsuarioRoles.SingleAsync(x => x.UsuarioId == 2)).EmpresaId = 2;
            if (cambio == "usuario") (await db.Usuarios.FindAsync(2L))!.Activo = false;
            if (cambio == "empresa") (await db.Empresas.FindAsync(1L))!.Activo = false;
            if (cambio == "rol") (await db.Roles.FindAsync(2L))!.Activo = false;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = sesion.GetProperty("refresh_token").GetString() })).StatusCode);
    }

    [Fact]
    public async Task Refresh_RotaConservaEmpresaYLogoutRevocaJwt()
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        var sesion = await Login(client, "vendedor@segunda.com", "VendedorSegunda123!");
        var refresh = sesion.GetProperty("refresh_token").GetString();
        var renewed = await client.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = refresh });
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var nueva = await renewed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, nueva.GetProperty("usuario").GetProperty("empresa_id").GetInt64());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sesion/yo")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = refresh })).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nueva.GetProperty("access_token").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/sesion/yo")).StatusCode);
    }

    [Fact]
    public async Task CambioDeRolPorAdmin_RevocaJwtYRefreshDelUsuarioDeSuEmpresa()
    {
        using var app = new TenantApplication(); using var admin = app.CreateClient(); using var vendedor = app.CreateClient(); await app.Seed();
        await Login(admin, "admin@vargas.com", "Admin123!");
        var sesion = await Login(vendedor, "vendedor@vargas.com", "Vendedor123!");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PutAsJsonAsync("/api/usuarios/2/rol", new { rol_id = 3 })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await vendedor.GetAsync("/api/productos")).StatusCode);
        vendedor.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await vendedor.PostAsJsonAsync("/api/auth/refresh", new { refresh_token = sesion.GetProperty("refresh_token").GetString() })).StatusCode);
    }

    [Fact]
    public async Task CajaConservaAmbitoYEstadoPeroNoPdfNiProductos()
    {
        using var app = new TenantApplication(); using var client = app.CreateClient(); await app.Seed();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.UsuarioRoles.SingleAsync(x => x.UsuarioId == 2)).RolId = 3; await db.SaveChangesAsync();
        }
        await Login(client, "vendedor@vargas.com", "Vendedor123!");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/pedidos/1002/estado", new { estado = "Listo" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/pedidos/2001")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/pedidos/1001/comprobante")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/productos", Form())).StatusCode);
    }

    [Fact]
    public async Task BaseDeDatos_RechazaItemsDeOtraEmpresaYRolGlobalConEmpresa()
    {
        using var scope = new BackendScope();
        scope.Db.Empresas.Add(new Empresa { Id = 2, NombreEmpresa = "Segunda" }); await scope.Db.SaveChangesAsync();
        var ajeno = await scope.Seed(empresaId: 2); var pedido = await scope.CrearPedido();
        scope.Db.ChangeTracker.Clear();
        scope.Db.PedidoItems.Add(new PedidoItem { PedidoId = pedido.Id, EmpresaId = 1, ProductoId = ajeno.Id, Nombre = "Ajeno", Cantidad = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => scope.Db.SaveChangesAsync());
        scope.Db.ChangeTracker.Clear();
        scope.Db.PedidoItems.Add(new PedidoItem { PedidoId = pedido.Id, EmpresaId = 2, ProductoId = ajeno.Id, Nombre = "Ajeno", Cantidad = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => scope.Db.SaveChangesAsync());
        scope.Db.ChangeTracker.Clear();
        scope.Db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = 3, RolId = 4, EmpresaId = 2 });
        await Assert.ThrowsAsync<DbUpdateException>(() => scope.Db.SaveChangesAsync());
    }

    private static MultipartFormDataContent Form(long? empresa = null)
    {
        var form = new MultipartFormDataContent { { new StringContent("Producto"), "nombre" }, { new StringContent("10"), "precio" }, { new StringContent("true"), "stock" } };
        if (empresa is not null) form.Add(new StringContent(empresa.ToString()!), "empresaId");
        return form;
    }
    private static async Task<JsonElement> Login(HttpClient client, string email, string password, long? empresa = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password, empresa_id = empresa });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sesion = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("access_token").GetString());
        return sesion;
    }
}

internal sealed class TenantApplication : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly string root = Path.Combine(@"C:\Users\Nahu\AppData\Local\Temp\opencode", "tenant-api-tests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(root); connection.Open();
        builder.UseEnvironment("Testing").UseContentRoot(root);
        builder.UseSetting("ConnectionStrings:Default", "Server=unused;Database=unused;User=unused;Password=unused");
        builder.UseSetting("Jwt:Key", "integration-tests-only-key-at-least-32-characters");
        builder.UseSetting("Jwt:Issuer", "tests"); builder.UseSetting("Jwt:Audience", "tests");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:Default"] = "Server=unused;Database=unused;User=unused;Password=unused",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Jwt:Key"] = "integration-tests-only-key-at-least-32-characters", ["Jwt:Issuer"] = "tests", ["Jwt:Audience"] = "tests" }));
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(x => x.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                x.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>) || x.ServiceType == typeof(AppDbContext)).ToArray()) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection).ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>());
        });
    }
    public async Task Seed()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        using var scope = Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Empresas.Add(new Empresa { Id = 2, NombreEmpresa = "Segunda" });
        db.Usuarios.AddRange(
            new Usuario { Id = 5, Nombre = "Admin Segunda", Email = "admin@segunda.com", PasswordHash = PasswordService.Hash("AdminSegunda123!") },
            new Usuario { Id = 6, Nombre = "Vendedor Segunda", Email = "vendedor@segunda.com", PasswordHash = PasswordService.Hash("VendedorSegunda123!") },
            new Usuario { Id = 7, Nombre = "Otro comun", Email = "otro@test.com", PasswordHash = PasswordService.Hash("Otro123!") },
            new Usuario { Id = 8, Nombre = "Global", Email = "global@test.com", PasswordHash = PasswordService.Hash("Global123!") });
        db.UsuarioRoles.AddRange(new UsuarioRol { UsuarioId = 1, RolId = 1, EmpresaId = 1 }, new UsuarioRol { UsuarioId = 2, RolId = 2, EmpresaId = 1 },
            new UsuarioRol { UsuarioId = 5, RolId = 1, EmpresaId = 2 }, new UsuarioRol { UsuarioId = 6, RolId = 2, EmpresaId = 2 }, new UsuarioRol { UsuarioId = 8, RolId = 4 });
        db.Productos.AddRange(new Producto { Id = 101, EmpresaId = 1, Nombre = "Producto Vargas", Precio = 10 }, new Producto { Id = 201, EmpresaId = 2, Nombre = "Producto Segunda", Precio = 20 });
        db.Pedidos.AddRange(new Pedido { Id = 1001, EmpresaId = 1, UsuarioId = 3, Cliente = "Ana", Estado = "Listo", Fecha = DateTime.UtcNow },
            new Pedido { Id = 1002, EmpresaId = 1, UsuarioId = 7, Cliente = "Otro", Fecha = DateTime.UtcNow },
            new Pedido { Id = 2001, EmpresaId = 2, UsuarioId = 3, Cliente = "Ana", Estado = "Listo", Fecha = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing); if (disposing) { connection.Dispose(); if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
