using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Data;

public static class SeedData
{
    public static async Task CorrerAsync(AppDbContext db)
    {
        // --- ROLES ---
        // Códigos en minúscula, exactamente como están en tu phpMyAdmin
        await AsegurarRol(db, 1, "Administrador", "administrador");
        await AsegurarRol(db, 2, "Vendedor", "vendedor");
        await AsegurarRol(db, 3, "Caja", "caja");
        await AsegurarRol(db, 4, "Superadmin", "superadmin");
        await db.SaveChangesAsync();

        // --- EMPRESAS ---
        if (!await db.Empresas.AnyAsync(x => x.Id == 1))
            db.Empresas.Add(new Empresa { Id = 1, NombreEmpresa = "Vargas", Activo = true });
        if (!await db.Empresas.AnyAsync(x => x.Id == 2))
            db.Empresas.Add(new Empresa { Id = 2, NombreEmpresa = "Segunda", Activo = true });
        await db.SaveChangesAsync();

        // --- USUARIOS ---
        await AsegurarUsuario(db, 1, "Administrador", "admin@vargas.com", "Admin123!");
        await AsegurarUsuario(db, 2, "Vendedor", "vendedor@vargas.com", "Vendedor123!");
        await AsegurarUsuario(db, 3, "Usuario sin rol", "sinrol@vargas.com", "SinRol123!");
        await AsegurarUsuario(db, 5, "Admin Segunda", "admin@segunda.com", "AdminSegunda123!");
        await AsegurarUsuario(db, 6, "Vendedor Segunda", "vendedor@segunda.com", "VendedorSegunda123!");
        await AsegurarUsuario(db, 7, "Otro comun", "otro@test.com", "Otro123!");
        // ESTE ES TU SUPER ADMIN
        await AsegurarUsuario(db, 8, "Global", "global@test.com", "Global123!");
        await db.SaveChangesAsync();

        // --- USUARIO_ROLES ---
        await AsegurarMembresia(db, usuarioId: 1, rolId: 1, empresaId: 1);
        await AsegurarMembresia(db, usuarioId: 2, rolId: 2, empresaId: 1);
        await AsegurarMembresia(db, usuarioId: 5, rolId: 1, empresaId: 2);
        await AsegurarMembresia(db, usuarioId: 6, rolId: 2, empresaId: 2);
        // SuperAdmin: SIN empresa, por eso es global
        await AsegurarMembresia(db, usuarioId: 8, rolId: 4, empresaId: null);
        await db.SaveChangesAsync();

        // --- PRODUCTOS / PEDIDOS de ejemplo ---
        if (!await db.Productos.AnyAsync(x => x.Id == 101))
            db.Productos.Add(new Producto { Id = 101, EmpresaId = 1, Nombre = "Producto Vargas", Precio = 10 });
        if (!await db.Productos.AnyAsync(x => x.Id == 201))
            db.Productos.Add(new Producto { Id = 201, EmpresaId = 2, Nombre = "Producto Segunda", Precio = 20 });

        if (!await db.Pedidos.AnyAsync(x => x.Id == 1001))
            db.Pedidos.Add(new Pedido { Id = 1001, EmpresaId = 1, UsuarioId = 3, Cliente = "Ana", Estado = "Listo", Fecha = DateTime.UtcNow });
        if (!await db.Pedidos.AnyAsync(x => x.Id == 1002))
            db.Pedidos.Add(new Pedido { Id = 1002, EmpresaId = 1, UsuarioId = 7, Cliente = "Otro", Fecha = DateTime.UtcNow });
        if (!await db.Pedidos.AnyAsync(x => x.Id == 2001))
            db.Pedidos.Add(new Pedido { Id = 2001, EmpresaId = 2, UsuarioId = 3, Cliente = "Ana", Estado = "Listo", Fecha = DateTime.UtcNow });

        await db.SaveChangesAsync();
    }

    private static async Task AsegurarRol(AppDbContext db, long id, string nombre, string codigo)
    {
        if (!await db.Roles.AnyAsync(x => x.Id == id))
            db.Roles.Add(new Rol { Id = id, Nombre = nombre, Codigo = codigo, Activo = true });
    }

    private static async Task AsegurarUsuario(AppDbContext db, long id, string nombre, string email, string password)
    {
        if (!await db.Usuarios.AnyAsync(x => x.Id == id))
        {
            db.Usuarios.Add(new Usuario
            {
                Id = id,
                Nombre = nombre,
                Email = email,
                PasswordHash = PasswordService.Hash(password),
                Activo = true
            });
        }
    }

    private static async Task AsegurarMembresia(AppDbContext db, long usuarioId, long rolId, long? empresaId)
    {
        if (!await db.UsuarioRoles.AnyAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId && x.EmpresaId == empresaId))
        {
            db.UsuarioRoles.Add(new UsuarioRol
            {
                UsuarioId = usuarioId,
                RolId = rolId,
                EmpresaId = empresaId
            });
        }
    }
}