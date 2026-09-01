using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.Nombre)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasMaxLength(1000);

        builder.Property(x => x.Precio)
            .HasPrecision(12, 2);

        builder.Property(x => x.Stock)
            .IsRequired();
    }
}