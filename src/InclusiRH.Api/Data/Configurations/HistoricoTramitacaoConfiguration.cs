using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class HistoricoTramitacaoConfiguration : IEntityTypeConfiguration<HistoricoTramitacao>
{
    public void Configure(EntityTypeBuilder<HistoricoTramitacao> builder)
    {
        builder.ToTable("HistoricosTramitacao");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ParecerRH)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(h => h.EvidenciasAnexadas)
            .HasMaxLength(4000);

        builder.Property(h => h.DataTramitacao)
            .IsRequired();

        builder.Property(h => h.Autor)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.StatusResultante)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(h => h.Denuncia)
            .WithMany(d => d.Historicos)
            .HasForeignKey(h => h.DenunciaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.DenunciaId);
    }
}
