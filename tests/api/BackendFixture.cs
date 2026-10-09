using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using multitenant_vargas.Api.Controllers;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;
using Xunit;

namespace Unidad6.Tests;

public sealed class BackendFixture
{
    public BackendFixture() => QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
}

internal sealed class BackendScope : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> options;
    public string Root { get; }
    public AppDbContext Db { get; }
    public SaveFailure Failure { get; } = new();
    public ProductoFotoService Fotos { get; }
    public ProductosController Productos { get; }
    public PedidosController Pedidos { get; }

    public BackendScope()
    {
        const string approved = @"C:\Users\Nahu\AppData\Local\Temp\opencode";
        if (!Directory.Exists(approved))
            throw new DirectoryNotFoundException("El directorio temporal aprobado no existe.");
        Root = Path.Combine(approved, "unidad6-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        connection.Open();
        options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>()
            .AddInterceptors(Failure)
            .Options;
        Db = new AppDbContext(options);
        Db.Database.EnsureCreated();
        Db.UsuarioRoles.AddRange(new UsuarioRol { UsuarioId = 1, RolId = 1, EmpresaId = 1 },
            new UsuarioRol { UsuarioId = 2, RolId = 2, EmpresaId = 1 });
        Db.SaveChanges();
        Fotos = new ProductoFotoService(new TestEnvironment(Root), NullLogger<ProductoFotoService>.Instance);
        Productos = new ProductosController(Db, Fotos, NullLogger<ProductosController>.Instance);
        Autenticar(Productos);
        Pedidos = Controller(Db);
    }

    public AppDbContext NewContext() => new(options);
    public static PedidosController Controller(AppDbContext db)
    {
        var controller = new PedidosController(db, NullLogger<PedidosController>.Instance);
        Autenticar(controller);
        return controller;
    }
    public static void Autenticar(ControllerBase controller, long usuarioId = 1, string? rol = "administrador", long? empresaId = 1)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, usuarioId.ToString()) };
        if (rol is not null) claims.Add(new(ClaimTypes.Role, rol));
        if (empresaId is not null) claims.Add(new("empresa_id", empresaId.ToString()!));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) } };
    }
    public string FotoPath(string url) => Path.Combine(Root, "uploads", "productos", Path.GetFileName(url));

    public async Task<Producto> Seed(decimal precio = 12.50m, bool stock = true, string nombre = "Producto", long empresaId = 1)
    {
        var producto = new Producto { Nombre = nombre, Precio = precio, Stock = stock, EmpresaId = empresaId };
        Db.Productos.Add(producto);
        await Db.SaveChangesAsync();
        return producto;
    }

    public async Task<PedidoDetalleResponse> CrearPedido(int cantidad = 2)
    {
        var producto = await Seed();
        var result = await Pedidos.Crear(new PedidoRequest(" Cliente ", [new(producto.Id, cantidad)]), default);
        return Assert.IsType<PedidoDetalleResponse>(Assert.IsType<CreatedAtActionResult>(result).Value);
    }

    public static void Error<T>(IActionResult result, int status, string codigo) where T : ObjectResult
    {
        var error = Assert.IsType<T>(result);
        Assert.Equal(status, error.StatusCode);
        Assert.NotNull(error.Value);
        Assert.Equal(codigo, error.Value.GetType().GetProperty("codigo")!.GetValue(error.Value));
    }

    public void Dispose()
    {
        Db.Dispose();
        connection.Dispose();
        Directory.Delete(Root, recursive: true);
    }
}

// Only translate the MySQL-specific CHECK syntax; retain the production relationships and concurrency token.
public sealed class SqliteTestModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        modelBuilder.Entity<Pedido>().ToTable("pedidos", table => table.HasCheckConstraint(
            "ck_pedidos_estado", "\"Estado\" COLLATE BINARY IN ('En preparacion', 'Listo', 'Cancelado')"));
        modelBuilder.Entity<UsuarioRol>().ToTable("usuario_roles", table => table.HasCheckConstraint(
            "ck_usuario_roles_ambito", "(\"RolId\" = 4 AND \"EmpresaId\" IS NULL) OR (\"RolId\" IN (1, 2, 3) AND \"EmpresaId\" IS NOT NULL)"));
        modelBuilder.Entity<RefreshToken>().Property(x => x.CreadoEn).HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
        modelBuilder.Entity<RefreshToken>().Property(x => x.ExpiraEn).HasConversion(x => x.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
        modelBuilder.Entity<RefreshToken>().Property(x => x.RevocadoEn).HasConversion(x => x!.Value.UtcTicks, x => new DateTimeOffset(x, TimeSpan.Zero));
    }
}

internal sealed class SaveFailure : SaveChangesInterceptor
{
    public bool Enabled { get; set; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Enabled) throw new DbUpdateException("Fallo de persistencia simulado por el test.");
        return ValueTask.FromResult(result);
    }
}

internal sealed class TestEnvironment(string root) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "Unidad6.Tests";
    public string EnvironmentName { get; set; } = "Testing";
    public string ContentRootPath { get; set; } = root;
    public string WebRootPath { get; set; } = root;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}
