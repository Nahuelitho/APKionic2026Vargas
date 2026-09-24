namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Usuario
{
    public long Id { get; set; }
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public bool Activo { get; set; } = true;
    public long? RolId { get; set; }
    public Rol? Rol { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
