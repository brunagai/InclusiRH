using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class DenunciaConfiguration : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> builder)
    {
        builder.ToTable("Denuncias");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Protocolo)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(d => d.Protocolo)
            .IsUnique();

        builder.Property(d => d.TipoOcorrencia)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Pacote AES-GCM: até 4000 caracteres em UTF-8 (até 16 000 bytes)
        // somados a 29 bytes de cabeçalho. Passa de 8000, então é varbinary(max).
        builder.Property(d => d.DescricaoProtegida)
            .HasColumnType("varbinary(max)")
            .IsRequired();

        builder.Property(d => d.IsAnonima)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.DataCriacao)
            .IsRequired();

        builder.Property(d => d.JustificativaContestacaoProtegida)
            .HasColumnType("varbinary(max)");

        builder.Property(d => d.VersaoLinha)
            .IsRowVersion();

        builder.HasIndex(d => new { d.Status, d.DataCriacao });

        builder.HasOne(d => d.Colaborador)
            .WithMany(c => c.Denuncias)
            .HasForeignKey(d => d.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
