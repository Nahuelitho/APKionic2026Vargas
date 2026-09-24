namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Rol
{
    public long Id { get; set; }
    public required string Nombre { get; set; }
    public required string Codigo { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
