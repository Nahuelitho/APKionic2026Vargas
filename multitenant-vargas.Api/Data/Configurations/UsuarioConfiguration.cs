using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;
using multitenant_vargas.Api.Services;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId);

        builder.HasData(
            new Usuario { Id = 1, Nombre = "Administrador", Email = "admin@vargas.com", PasswordHash = PasswordService.HashDeterministico("Admin123!", "admin-vargas"), RolId = 1, Activo = true },
            new Usuario { Id = 2, Nombre = "Vendedor", Email = "vendedor@vargas.com", PasswordHash = PasswordService.HashDeterministico("Vendedor123!", "vendedor-vargas"), RolId = 2, Activo = true },
            new Usuario { Id = 3, Nombre = "Usuario sin rol", Email = "sinrol@vargas.com", PasswordHash = PasswordService.HashDeterministico("SinRol123!", "sinrol-vargas"), RolId = null, Activo = true },
            // Cuenta global para gestionar todas las empresas. Su ambito real es
            // la membresia con rol 4 y empresa nula (ver UsuarioRolConfiguration).
            new Usuario { Id = 9, Nombre = "Superadmin", Email = "superadmin@vargas.com", PasswordHash = PasswordService.HashDeterministico("SuperAdmin123!", "superadmin-vargas"), RolId = 4, Activo = true }
        );
    }
}
