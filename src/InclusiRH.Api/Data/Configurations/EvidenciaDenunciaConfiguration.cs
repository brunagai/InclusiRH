using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class EvidenciaDenunciaConfiguration : IEntityTypeConfiguration<EvidenciaDenuncia>
{
    public void Configure(EntityTypeBuilder<EvidenciaDenuncia> builder)
    {
        builder.ToTable("EvidenciasDenuncia");

        builder.HasKey(e => e.Id);

        // O Id é gerado pela aplicação antes de gravar o arquivo, porque
        // também nomeia o arquivo e entra na verificação da cifra.
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.NomeArquivo)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.CaminhoArquivo)
            .HasMaxLength(260)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(e => e.CaminhoArquivo)
            .IsUnique();

        builder.Property(e => e.TipoArquivo)
            .HasMaxLength(100)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(e => e.Origem)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.DataUpload)
            .IsRequired();

        builder.HasOne(e => e.Denuncia)
            .WithMany(d => d.Evidencias)
            .HasForeignKey(e => e.DenunciaId)
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction: a denúncia já apaga histórico e evidências em cascata.
        // Um segundo caminho de cascata pelo histórico é recusado pelo SQL Server.
        builder.HasOne(e => e.HistoricoTramitacao)
            .WithMany(h => h.Evidencias)
            .HasForeignKey(e => e.HistoricoTramitacaoId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
