using InclusiRH.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Data;

public sealed class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Colaborador> Colaboradores => Set<Colaborador>();

    public DbSet<Autodeclaracao> Autodeclaracoes => Set<Autodeclaracao>();

    public DbSet<Denuncia> Denuncias => Set<Denuncia>();

    public DbSet<HistoricoTramitacao> HistoricosTramitacao => Set<HistoricoTramitacao>();

    public DbSet<EvidenciaDenuncia> EvidenciasDenuncia => Set<EvidenciaDenuncia>();

    public DbSet<Treinamento> Treinamentos => Set<Treinamento>();

    public DbSet<ProgressoTreinamento> ProgressosTreinamento => Set<ProgressoTreinamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
