using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using multitenant_vargas.Api.Data;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Services;

public sealed record AmbitoSesion(string? Rol, long? EmpresaId);

public static class AmbitoService
{
    public static long UsuarioId(ClaimsPrincipal user) => long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static long? EmpresaId(ClaimsPrincipal user) => long.TryParse(user.FindFirstValue("empresa_id"), out var id) ? id : null;
    public static bool Global(ClaimsPrincipal user) => user.IsInRole("superadmin");
    public static bool Gestion(ClaimsPrincipal user) => Global(user) || EmpresaId(user) is not null;

    public static IQueryable<Producto> Catalogo(AppDbContext db, ClaimsPrincipal user) =>
        db.Productos.Where(x => (x.Empresa.Activo || Global(user)) && (!Gestion(user) || Global(user) || x.EmpresaId == EmpresaId(user)));

    public static IQueryable<Pedido> Pedidos(AppDbContext db, ClaimsPrincipal user)
    {
        var id = UsuarioId(user);
        var empresa = EmpresaId(user);
        return Global(user) ? db.Pedidos : empresa is not null
            ? db.Pedidos.Where(x => x.EmpresaId == empresa)
            : db.Pedidos.Where(x => x.UsuarioId == id);
    }

    public static IQueryable<Usuario> UsuariosConRoles(AppDbContext db) => db.Usuarios
        .Include(x => x.UsuarioRoles).ThenInclude(x => x.Rol)
        .Include(x => x.UsuarioRoles).ThenInclude(x => x.Empresa);

    public static AmbitoSesion Elegir(Usuario usuario, long? empresa = null)
    {
        var roles = usuario.UsuarioRoles.Where(x => x.Rol.Activo && (x.EmpresaId is null || x.Empresa is { Activo: true }));
        if (roles.Any(x => x.Rol.Codigo == "superadmin" && x.EmpresaId is null)) return new("superadmin", null);
        var membresia = roles.Where(x => x.EmpresaId is not null && (empresa is null || x.EmpresaId == empresa)).OrderBy(x => x.EmpresaId).FirstOrDefault();
        return membresia is null ? new(null, null) : new(membresia.Rol.Codigo, membresia.EmpresaId);
    }

    public static bool Valido(Usuario usuario, AmbitoSesion ambito) => usuario.Activo &&
        (ambito.Rol is null && ambito.EmpresaId is null ? usuario.UsuarioRoles.Count == 0 :
        usuario.UsuarioRoles.Any(x => x.Rol.Activo && x.Rol.Codigo == ambito.Rol && x.EmpresaId == ambito.EmpresaId &&
            (ambito.Rol == "superadmin" ? x.EmpresaId is null : x.Empresa is { Activo: true })));
}
