using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("pedidos", table => table.HasCheckConstraint("ck_pedidos_estado",
            "BINARY estado IN ('En preparacion', 'Listo', 'Cancelado')"));
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.Id, x.EmpresaId });
        builder.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Cliente).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Estado).HasMaxLength(20).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.HasIndex(x => x.Fecha);
        builder.HasMany(x => x.Items).WithOne(x => x.Pedido).HasForeignKey(x => new { x.PedidoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PedidoItemConfiguration : IEntityTypeConfiguration<PedidoItem>
{
    public void Configure(EntityTypeBuilder<PedidoItem> builder)
    {
        builder.ToTable("pedido_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PrecioUnitario).HasPrecision(12, 2);
        builder.Property(x => x.Subtotal).HasPrecision(12, 2);
        // El historial conserva nombre/precio aunque se elimine el producto del catalogo.
        builder.HasOne(x => x.Producto).WithMany().HasForeignKey(x => new { x.ProductoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId }).OnDelete(DeleteBehavior.Restrict);
    }
}
