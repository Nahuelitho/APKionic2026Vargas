using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using multitenant_vargas.Api.Controllers;
using Xunit;

namespace Unidad6.Tests;

public sealed class PermisosTests
{
    [Theory]
    [InlineData(typeof(PedidosController), null, null)]
    [InlineData(typeof(PedidosController), nameof(PedidosController.Crear), null)]
    [InlineData(typeof(PedidosController), nameof(PedidosController.CambiarEstado), "superadmin,administrador,vendedor,caja")]
    [InlineData(typeof(PedidosController), nameof(PedidosController.Comprobante), "superadmin,administrador,vendedor")]
    [InlineData(typeof(ProductosController), null, null)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.Crear), "superadmin,administrador,vendedor")]
    [InlineData(typeof(ProductosController), nameof(ProductosController.Actualizar), "superadmin,administrador,vendedor")]
    [InlineData(typeof(ProductosController), nameof(ProductosController.Eliminar), "superadmin,administrador,vendedor")]
    [InlineData(typeof(UsuariosController), null, "superadmin,administrador")]
    [InlineData(typeof(EmpresasController), nameof(EmpresasController.Crear), "superadmin")]
    public void RolesDeclarados_CoincidenConContrato(Type controller, string? method, string? roles)
    {
        MemberInfo member = method is null ? controller : controller.GetMethod(method)!;
        Assert.Equal(roles, Assert.Single(member.GetCustomAttributes<AuthorizeAttribute>()).Roles);
        Assert.Empty(controller.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Empty(member.GetCustomAttributes<AllowAnonymousAttribute>());
    }
}
