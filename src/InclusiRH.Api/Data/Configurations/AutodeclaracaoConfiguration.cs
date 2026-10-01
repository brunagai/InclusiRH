using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class AutodeclaracaoConfiguration : IEntityTypeConfiguration<Autodeclaracao>
{
    public void Configure(EntityTypeBuilder<Autodeclaracao> builder)
    {
        builder.ToTable("Autodeclaracoes");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DadosProtegidos)
            .IsRequired();

        builder.Property(a => a.ConsentimentoLgpd)
            .IsRequired();

        builder.Property(a => a.DataConsentimento)
            .IsRequired();

        builder.Property(a => a.DataAtualizacao)
            .IsRequired();

        builder.HasOne(a => a.Colaborador)
            .WithOne(c => c.Autodeclaracao)
            .HasForeignKey<Autodeclaracao>(a => a.ColaboradorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ColaboradorId)
            .IsUnique();
    }
}
