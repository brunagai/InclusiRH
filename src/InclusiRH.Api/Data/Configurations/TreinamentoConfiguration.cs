using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class TreinamentoConfiguration : IEntityTypeConfiguration<Treinamento>
{
    public void Configure(EntityTypeBuilder<Treinamento> builder)
    {
        builder.ToTable("Treinamentos");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Titulo)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Descricao)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(t => t.Categoria)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.CargaHoraria)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(t => t.Modulo)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Conteudo)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(t => t.Obrigatorio)
            .IsRequired();

        builder.Property(t => t.Ativo)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
