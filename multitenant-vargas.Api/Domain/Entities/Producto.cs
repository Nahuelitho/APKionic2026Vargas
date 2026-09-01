namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Producto
{
    public long Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public bool Stock { get; set; } = true;
}