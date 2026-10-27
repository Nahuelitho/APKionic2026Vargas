namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Notificacion
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }          // destinatario
    public Usuario Usuario { get; set; } = null!;
    public long EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public long? PedidoId { get; set; }          // sin FK: es solo referencia
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime Fecha { get; set; }
}