namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Empresa
{
    public long Id { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}

public sealed class UsuarioRol
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public long RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public long? EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
