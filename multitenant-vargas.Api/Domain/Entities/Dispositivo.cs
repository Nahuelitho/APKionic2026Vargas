namespace multitenant_vargas.Api.Domain.Entities;

/// <summary>
/// Telefono (token FCM) que recibe las push de un usuario.
/// La app lo da de alta al entrar y lo borra al salir.
/// </summary>
public sealed class Dispositivo
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string Plataforma { get; set; } = "android";
    public DateTime CreadoEn { get; set; }
    public DateTime ActualizadoEn { get; set; }
}
