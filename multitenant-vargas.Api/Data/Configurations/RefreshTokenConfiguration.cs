using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using multitenant_vargas.Api.Domain.Entities;

namespace multitenant_vargas.Api.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasOne(x => x.Usuario).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UsuarioId);
    }
}
