using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.HasData(
            new Rol { Id = 1, Nombre = "Administrador", Codigo = "administrador", Activo = true },
            new Rol { Id = 2, Nombre = "Vendedor", Codigo = "vendedor", Activo = true },
            new Rol { Id = 3, Nombre = "Caja", Codigo = "caja", Activo = true }
        );
    }
}
