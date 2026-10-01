using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class ColaboradorConfiguration : IEntityTypeConfiguration<Colaborador>
{
    public void Configure(EntityTypeBuilder<Colaborador> builder)
    {
        builder.ToTable("Colaboradores");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nome)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.SenhaHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.Cargo)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Departamento)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(c => c.Papel)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(c => c.Ativo)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(c => c.DataCadastro)
            .IsRequired();

        // SHA-256 em Base64 sempre tem 44 caracteres.
        builder.Property(c => c.RefreshToken)
            .HasMaxLength(44)
            .IsUnicode(false);

        builder.Property(c => c.RefreshTokenExpiryTime);
    }
}
