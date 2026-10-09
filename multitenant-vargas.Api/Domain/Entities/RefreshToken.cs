namespace multitenant_vargas.Api.Domain.Entities;

public sealed class RefreshToken
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public long? EmpresaId { get; set; }
    public string? RolSesion { get; set; }
    public int VersionAmbito { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public required string TokenHash { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadoEn { get; set; }
}
