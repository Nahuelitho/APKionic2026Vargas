using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NombreEmpresa).HasMaxLength(160).IsRequired();
        builder.HasData(new Empresa { Id = 1, NombreEmpresa = "Vargas", Activo = true });
    }
}

public sealed class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("usuario_roles", table => table.HasCheckConstraint("ck_usuario_roles_ambito",
            "(rol_id = 4 AND empresa_id IS NULL) OR (rol_id IN (1, 2, 3) AND empresa_id IS NOT NULL)"));
        builder.HasKey(x => x.Id);
        // One effective role per user/company; the global scope is reserved for superadmin.
        builder.HasIndex(x => new { x.UsuarioId, x.EmpresaId }).IsUnique();
        builder.HasOne(x => x.Usuario).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Rol).WithMany().HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}
