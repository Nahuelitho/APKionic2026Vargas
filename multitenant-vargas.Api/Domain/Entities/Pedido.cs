namespace multitenant_vargas.Api.Domain.Entities;

public sealed class Pedido
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public long UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string Cliente { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "En preparacion";
    public decimal Total { get; set; }
    public List<PedidoItem> Items { get; set; } = [];
}

public sealed class PedidoItem
{
    public long Id { get; set; }
    public long PedidoId { get; set; }
    public long EmpresaId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public long? ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Subtotal { get; set; }
}
