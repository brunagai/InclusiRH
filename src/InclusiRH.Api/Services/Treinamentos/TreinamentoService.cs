using InclusiRH.Api.Data;
using InclusiRH.Api.DTOs.Treinamentos;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Services.Treinamentos;

public sealed class TreinamentoService : ITreinamentoService
{
    private const string BaseLegal =
        "Leis nº 10.639/2003 e nº 11.645/2008. A taxa considera os treinamentos ativos marcados como obrigatórios, por departamento.";

    private readonly ApplicationDbContext _db;

    public TreinamentoService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TreinamentoResponse>> ListarDisponiveisAsync(CancellationToken cancellationToken)
    {
        var treinamentos = await _db.Treinamentos
            .AsNoTracking()
            .Where(t => t.Ativo)
            .OrderBy(t => t.Titulo)
            .ToListAsync(cancellationToken);

        return treinamentos.Select(Mapear).ToList();
    }

    public async Task<TreinamentoResponse> CadastrarAsync(CriarTreinamentoDto dto, CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(dto.Categoria, "categoria");

        var titulo = dto.Titulo.Trim();
        var descricao = dto.Descricao.Trim();
        var modulo = dto.Modulo.Trim();
        var conteudo = dto.Conteudo.Trim();
        if (titulo.Length == 0 || descricao.Length == 0 || modulo.Length == 0 || conteudo.Length == 0)
        {
            throw new RegraDeNegocioException("Título, descrição, módulo e conteúdo são obrigatórios.");
        }

        if (dto.CargaHoraria is < 0.5m or > 400m)
        {
            throw new RegraDeNegocioException("A carga horária deve estar entre 0,5 e 400 horas.");
        }

        var treinamento = new Treinamento
        {
            Id = Guid.NewGuid(),
            Titulo = titulo,
            Descricao = descricao,
            Categoria = dto.Categoria,
            CargaHoraria = dto.CargaHoraria,
            Modulo = modulo,
            Conteudo = conteudo,
            Obrigatorio = dto.Obrigatorio,
            Ativo = true
        };

        _db.Treinamentos.Add(treinamento);
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(treinamento);
    }

    public async Task<ProgressoTreinamentoResponse> AtualizarProgressoAsync(
        Guid treinamentoId,
        Guid colaboradorId,
        AtualizarProgressoDto dto,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(dto.Status, "status");

        var treinamento = await _db.Treinamentos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == treinamentoId && t.Ativo, cancellationToken);

        if (treinamento is null)
        {
            throw new RegraDeNegocioException("Treinamento não encontrado ou inativo.");
        }

        var colaboradorAtivo = await _db.Colaboradores
            .AnyAsync(c => c.Id == colaboradorId && c.Ativo, cancellationToken);

        if (!colaboradorAtivo)
        {
            throw new RegraDeNegocioException("Colaborador não encontrado ou inativo.");
        }

        var progresso = await _db.ProgressosTreinamento
            .FirstOrDefaultAsync(
                p => p.TreinamentoId == treinamentoId && p.ColaboradorId == colaboradorId,
                cancellationToken);

        if (progresso is null)
        {
            progresso = new ProgressoTreinamento
            {
                Id = Guid.NewGuid(),
                TreinamentoId = treinamentoId,
                ColaboradorId = colaboradorId,
                Status = StatusConclusaoTreinamento.NaoIniciado
            };
            _db.ProgressosTreinamento.Add(progresso);
        }

        AplicarStatus(progresso, dto.Status);
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(progresso, treinamento);
    }

    public async Task<IReadOnlyList<ProgressoTreinamentoResponse>> ListarPorColaboradorAsync(
        Guid colaboradorId,
        CancellationToken cancellationToken)
    {
        var progressos = await _db.ProgressosTreinamento
            .AsNoTracking()
            .Include(p => p.Treinamento)
            .Where(p => p.ColaboradorId == colaboradorId)
            .OrderBy(p => p.Treinamento.Titulo)
            .ToListAsync(cancellationToken);

        return progressos.Select(p => Mapear(p, p.Treinamento)).ToList();
    }

    public async Task<RelatorioConclusaoDto> GerarRelatorioConclusaoAsync(CancellationToken cancellationToken)
    {
        var obrigatorios = await _db.Treinamentos.CountAsync(t => t.Ativo && t.Obrigatorio, cancellationToken);
        var colaboradores = await _db.Colaboradores
            .AsNoTracking()
            .Where(c => c.Ativo)
            .Select(c => new { c.Id, c.Departamento })
            .ToListAsync(cancellationToken);

        var concluidos = await _db.ProgressosTreinamento
            .AsNoTracking()
            .Where(p =>
                p.Status == StatusConclusaoTreinamento.Concluido
                && p.Treinamento.Ativo
                && p.Treinamento.Obrigatorio
                && p.Colaborador.Ativo)
            .Select(p => new { p.ColaboradorId, p.Colaborador.Departamento })
            .ToListAsync(cancellationToken);

        var realizadosPorDepartamento = concluidos
            .GroupBy(item => item.Departamento)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var departamentos = colaboradores
            .GroupBy(c => c.Departamento)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo =>
            {
                var pessoas = grupo.Count();
                var esperadas = pessoas * obrigatorios;
                var realizadas = realizadosPorDepartamento.GetValueOrDefault(grupo.Key);
                return new ConclusaoDepartamentoDto
                {
                    Departamento = grupo.Key,
                    ColaboradoresAtivos = pessoas,
                    ConclusoesEsperadas = esperadas,
                    ConclusoesRealizadas = realizadas,
                    TaxaConclusao = Taxa(realizadas, esperadas)
                };
            })
            .ToList();

        var esperadasGeral = colaboradores.Count * obrigatorios;
        var realizadasGeral = concluidos.Count;

        return new RelatorioConclusaoDto
        {
            GeradoEm = DateTimeOffset.UtcNow,
            TreinamentosObrigatorios = obrigatorios,
            TaxaConclusaoGeral = Taxa(realizadasGeral, esperadasGeral),
            Aviso = obrigatorios == 0
                ? "Nenhum treinamento ativo está marcado como obrigatório."
                : null,
            Departamentos = departamentos,
            BaseLegal = BaseLegal
        };
    }

    private static void AplicarStatus(ProgressoTreinamento progresso, StatusConclusaoTreinamento novo)
    {
        if (progresso.Status == StatusConclusaoTreinamento.Concluido && novo != StatusConclusaoTreinamento.Concluido)
        {
            throw new RegraDeNegocioException("Um treinamento concluído não pode voltar a um status anterior.");
        }

        if (progresso.Status == StatusConclusaoTreinamento.EmAndamento && novo == StatusConclusaoTreinamento.NaoIniciado)
        {
            throw new RegraDeNegocioException("Um treinamento já iniciado não pode voltar para não iniciado.");
        }

        var agora = DateTimeOffset.UtcNow;
        if (novo == StatusConclusaoTreinamento.EmAndamento && progresso.DataInicio is null)
        {
            progresso.DataInicio = agora;
        }

        if (novo == StatusConclusaoTreinamento.Concluido)
        {
            progresso.DataInicio ??= agora;
            progresso.DataConclusao ??= agora;
        }

        progresso.Status = novo;
    }

    private static decimal Taxa(int realizadas, int esperadas) =>
        esperadas == 0 ? 0 : Math.Round(realizadas * 100m / esperadas, 1, MidpointRounding.AwayFromZero);

    private static TreinamentoResponse Mapear(Treinamento treinamento) => new()
    {
        Id = treinamento.Id,
        Titulo = treinamento.Titulo,
        Descricao = treinamento.Descricao,
        Categoria = treinamento.Categoria,
        CategoriaDescricao = DescricoesCategoriaTreinamento.Obter(treinamento.Categoria),
        CargaHoraria = treinamento.CargaHoraria,
        Modulo = treinamento.Modulo,
        Conteudo = treinamento.Conteudo,
        Obrigatorio = treinamento.Obrigatorio,
        Ativo = treinamento.Ativo
    };

    private static ProgressoTreinamentoResponse Mapear(ProgressoTreinamento progresso, Treinamento treinamento) => new()
    {
        Id = progresso.Id,
        TreinamentoId = treinamento.Id,
        Titulo = treinamento.Titulo,
        Categoria = treinamento.Categoria,
        CategoriaDescricao = DescricoesCategoriaTreinamento.Obter(treinamento.Categoria),
        ColaboradorId = progresso.ColaboradorId,
        Status = progresso.Status,
        DataInicio = progresso.DataInicio,
        DataConclusao = progresso.DataConclusao
    };
}
