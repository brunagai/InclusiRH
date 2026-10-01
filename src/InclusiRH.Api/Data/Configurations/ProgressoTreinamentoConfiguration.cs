using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InclusiRH.Api.Data.Configurations;

public sealed class ProgressoTreinamentoConfiguration : IEntityTypeConfiguration<ProgressoTreinamento>
{
    public void Configure(EntityTypeBuilder<ProgressoTreinamento> builder)
    {
        builder.ToTable("ProgressosTreinamento");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(p => new { p.TreinamentoId, p.ColaboradorId })
            .IsUnique();

        builder.HasOne(p => p.Treinamento)
            .WithMany(t => t.Progressos)
            .HasForeignKey(p => p.TreinamentoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Colaborador)
            .WithMany(c => c.ProgressosTreinamento)
            .HasForeignKey(p => p.ColaboradorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
