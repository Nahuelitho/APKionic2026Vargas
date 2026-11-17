using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class DispositivoConfiguration : IEntityTypeConfiguration<Dispositivo>
{
    public void Configure(EntityTypeBuilder<Dispositivo> builder)
    {
        builder.ToTable("dispositivos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Plataforma).HasMaxLength(20).IsRequired();
        builder.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        // Un token identifica un telefono: no puede pertenecer a dos usuarios.
        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UsuarioId);
    }
}
