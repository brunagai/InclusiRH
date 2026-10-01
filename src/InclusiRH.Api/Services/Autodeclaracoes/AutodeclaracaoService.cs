using System.Security.Cryptography;
using InclusiRH.Api.Data;
using InclusiRH.Api.DTOs.Autodeclaracoes;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Services.Autodeclaracoes;

public sealed class AutodeclaracaoService : IAutodeclaracaoService
{
    private const int LimiteSupressao = 5;

    private const string BaseLegal =
        "Base demográfica para a Lei 14.611/2023 (Igualdade Salarial) e a Lei 12.288/2010 (Estatuto da Igualdade Racial). O relatório não identifica pessoas.";

    private readonly ApplicationDbContext _db;
    private readonly IProtetorDadosSensiveis _protetor;
    private readonly ILogger<AutodeclaracaoService> _logger;

    public AutodeclaracaoService(
        ApplicationDbContext db,
        IProtetorDadosSensiveis protetor,
        ILogger<AutodeclaracaoService> logger)
    {
        _db = db;
        _protetor = protetor;
        _logger = logger;
    }

    public async Task<AutodeclaracaoDto?> ObterPropriaAsync(Guid colaboradorId, CancellationToken cancellationToken)
    {
        var registro = await CarregarAsync(colaboradorId, cancellationToken);
        return registro is null ? null : ParaDto(registro.Value.Entidade, registro.Value.Dados);
    }

    public Task<AutodeclaracaoDto> SalvarPropriaAsync(
        Guid colaboradorId,
        AutodeclaracaoDto dto,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(dto.Etnia, "etnia");
        Enumeracao.GarantirDefinida(dto.Genero, "gênero");
        Enumeracao.GarantirDefinida(dto.OrientacaoSexual, "orientação sexual");
        Enumeracao.GarantirDefinida(dto.ContextoSocioeconomico, "contexto socioeconômico");

        var situacao = dto.TemDeficiencia switch
        {
            true => SituacaoPcd.Sim,
            false => SituacaoPcd.Nao,
            null => SituacaoPcd.PrefiroNaoInformar
        };

        return SalvarInternoAsync(colaboradorId, new DadosAutodeclaracaoProtegidos
        {
            Etnia = dto.Etnia,
            Genero = dto.Genero,
            OrientacaoSexual = dto.OrientacaoSexual,
            SituacaoPcd = situacao,
            DescricaoDeficiencia = LimparDescricao(situacao, dto.DescricaoDeficiencia),
            ContextoSocioeconomico = dto.ContextoSocioeconomico
        }, dto.ConsentimentoLgpd, cancellationToken);
    }

    public async Task<RelatorioEsgDto> GerarRelatorioEsgAsync(CancellationToken cancellationToken)
    {
        var totalAtivos = await _db.Colaboradores.CountAsync(c => c.Ativo, cancellationToken);
        var pacotes = await _db.Autodeclaracoes
            .AsNoTracking()
            .Select(a => a.DadosProtegidos)
            .ToListAsync(cancellationToken);

        var lidos = new List<DadosAutodeclaracaoProtegidos>(pacotes.Count);
        foreach (var pacote in pacotes)
        {
            try
            {
                lidos.Add(_protetor.Revelar(pacote));
            }
            catch (CryptographicException)
            {
                _logger.LogWarning("Uma autodeclaração não pôde ser lida e ficou de fora do relatório agregado.");
            }
        }

        var etnia = Contar(lidos, item => item.Etnia.ToString());
        var genero = Contar(lidos, item => item.Genero.ToString());
        var pcd = Contar(lidos, item => item.SituacaoPcd.ToString());
        var contexto = Contar(lidos, item => item.ContextoSocioeconomico.ToString());
        var diversidade = MontarGrupo("Diversidade étnico-racial", etnia);
        var equidade = MontarGrupo("Equidade de gênero", genero);
        var inclusao = MontarGrupo("Inclusão de pessoas com deficiência", pcd);
        var contextoGrupo = MontarGrupo("Contexto socioeconômico", contexto);
        var baseSuficiente = lidos.Count >= LimiteSupressao;
        var algumGrupoSuprimido = !diversidade.Liberado || !equidade.Liberado || !inclusao.Liberado || !contextoGrupo.Liberado;

        return new RelatorioEsgDto
        {
            GeradoEm = DateTimeOffset.UtcNow,
            TotalColaboradoresAtivos = totalAtivos,
            TotalAutodeclaracoes = lidos.Count,
            PercentualCobertura = Percentual(lidos.Count, totalAtivos),
            IndicadoresLiberados = baseSuficiente,
            AvisoSupressao = !baseSuficiente
                ? "Há menos de 5 autodeclarações. Os percentuais foram omitidos para impedir reidentificação."
                : algumGrupoSuprimido
                    ? "Grupos com menos de 5 pessoas foram omitidos para impedir reidentificação."
                    : null,
            PercentualPopulacaoNegra = PercentualPopulacaoNegra(lidos, diversidade.Liberado),
            PercentualPcd = inclusao.Liberado ? PercentualPcd(lidos) : null,
            DiversidadeEtnicoRacial = diversidade,
            EquidadeDeGenero = equidade,
            InclusaoPcd = inclusao,
            ContextoSocioeconomico = contextoGrupo,
            BaseLegal = BaseLegal
        };
    }

    public async Task<AutodeclaracaoResponse?> ObterAsync(Guid colaboradorId, CancellationToken cancellationToken)
    {
        var registro = await CarregarAsync(colaboradorId, cancellationToken);
        return registro is null ? null : ParaResponse(registro.Value.Entidade, registro.Value.Dados);
    }

    public async Task<AutodeclaracaoResponse> SalvarAsync(
        Guid colaboradorId,
        SalvarAutodeclaracaoRequest request,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(request.Etnia, "etnia");
        Enumeracao.GarantirDefinida(request.Genero, "gênero");
        Enumeracao.GarantirDefinida(request.OrientacaoSexual, "orientação sexual");
        Enumeracao.GarantirDefinida(request.SituacaoPcd, "PCD");

        var existente = await CarregarRastreadoAsync(colaboradorId, cancellationToken);
        var contexto = existente?.Dados.ContextoSocioeconomico ?? ContextoSocioeconomico.PrefiroNaoInformar;

        var entidade = await PersistirAsync(colaboradorId, new DadosAutodeclaracaoProtegidos
        {
            Etnia = request.Etnia,
            Genero = request.Genero,
            OrientacaoSexual = request.OrientacaoSexual,
            SituacaoPcd = request.SituacaoPcd,
            DescricaoDeficiencia = LimparDescricao(request.SituacaoPcd, request.TipoDeficiencia),
            ContextoSocioeconomico = contexto
        }, request.ConsentimentoLgpd, existente?.Entidade, cancellationToken);

        return ParaResponse(entidade, _protetor.Revelar(entidade.DadosProtegidos));
    }

    private async Task<AutodeclaracaoDto> SalvarInternoAsync(
        Guid colaboradorId,
        DadosAutodeclaracaoProtegidos dados,
        bool consentimento,
        CancellationToken cancellationToken)
    {
        var existente = await CarregarRastreadoAsync(colaboradorId, cancellationToken);
        var entidade = await PersistirAsync(
            colaboradorId,
            dados,
            consentimento,
            existente?.Entidade,
            cancellationToken);

        return ParaDto(entidade, dados);
    }

    private async Task<Autodeclaracao> PersistirAsync(
        Guid colaboradorId,
        DadosAutodeclaracaoProtegidos dados,
        bool consentimento,
        Autodeclaracao? existente,
        CancellationToken cancellationToken)
    {
        if (!consentimento)
        {
            throw new RegraDeNegocioException(
                "O registro de dados sensíveis exige consentimento explícito, conforme a LGPD.");
        }

        var colaboradorAtivo = await _db.Colaboradores
            .AnyAsync(c => c.Id == colaboradorId && c.Ativo, cancellationToken);

        if (!colaboradorAtivo)
        {
            throw new RegraDeNegocioException("Colaborador não encontrado ou inativo.");
        }

        var agora = DateTimeOffset.UtcNow;
        var entidade = existente;
        if (entidade is null)
        {
            entidade = new Autodeclaracao
            {
                Id = Guid.NewGuid(),
                ColaboradorId = colaboradorId
            };
            _db.Autodeclaracoes.Add(entidade);
        }

        entidade.DadosProtegidos = _protetor.Proteger(dados);
        entidade.ConsentimentoLgpd = true;
        entidade.DataConsentimento = agora;
        entidade.DataAtualizacao = agora;
        await _db.SaveChangesAsync(cancellationToken);
        return entidade;
    }

    private async Task<(Autodeclaracao Entidade, DadosAutodeclaracaoProtegidos Dados)?> CarregarAsync(
        Guid colaboradorId,
        CancellationToken cancellationToken)
    {
        var entidade = await _db.Autodeclaracoes
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ColaboradorId == colaboradorId, cancellationToken);

        if (entidade is null)
        {
            return null;
        }

        return (entidade, _protetor.Revelar(entidade.DadosProtegidos));
    }

    private async Task<(Autodeclaracao Entidade, DadosAutodeclaracaoProtegidos Dados)?> CarregarRastreadoAsync(
        Guid colaboradorId,
        CancellationToken cancellationToken)
    {
        var entidade = await _db.Autodeclaracoes
            .FirstOrDefaultAsync(a => a.ColaboradorId == colaboradorId, cancellationToken);

        if (entidade is null)
        {
            return null;
        }

        return (entidade, _protetor.Revelar(entidade.DadosProtegidos));
    }

    private static Dictionary<string, int> Contar<T>(
        IEnumerable<DadosAutodeclaracaoProtegidos> itens,
        Func<DadosAutodeclaracaoProtegidos, T> seletor)
        where T : notnull
    {
        return itens
            .GroupBy(seletor)
            .ToDictionary(grupo => grupo.Key.ToString() ?? string.Empty, grupo => grupo.Count());
    }

    private static GrupoIndicadorDto MontarGrupo(string nome, IReadOnlyDictionary<string, int> contagens)
    {
        var baseCalculo = contagens.Values.Sum();
        var celulaPequena = contagens.Values.Any(quantidade => quantidade is > 0 and < LimiteSupressao);
        if (baseCalculo < LimiteSupressao || celulaPequena)
        {
            return new GrupoIndicadorDto
            {
                Nome = nome,
                BaseCalculo = baseCalculo,
                Liberado = false,
                MotivoSupressao = "Indicador omitido para evitar a reidentificação de pessoas em grupos com menos de 5 autodeclarações.",
                Categorias = []
            };
        }

        return new GrupoIndicadorDto
        {
            Nome = nome,
            BaseCalculo = baseCalculo,
            Liberado = true,
            Categorias = contagens
                .OrderBy(item => item.Key)
                .Select(item => new IndicadorPercentualDto
                {
                    Categoria = item.Key,
                    Quantidade = item.Value,
                    Percentual = Percentual(item.Value, baseCalculo)
                })
                .ToList()
        };
    }

    private static decimal? PercentualPopulacaoNegra(
        IReadOnlyList<DadosAutodeclaracaoProtegidos> itens,
        bool diversidadeLiberada)
    {
        if (!diversidadeLiberada)
        {
            return null;
        }

        var declararam = itens.Where(item => item.Etnia != Etnia.PrefiroNaoInformar).ToList();
        if (declararam.Count < LimiteSupressao)
        {
            return null;
        }

        var negra = declararam.Count(item => item.Etnia is Etnia.Preta or Etnia.Parda);
        if (negra is > 0 and < LimiteSupressao)
        {
            return null;
        }

        return Percentual(negra, declararam.Count);
    }

    private static decimal? PercentualPcd(IReadOnlyList<DadosAutodeclaracaoProtegidos> itens)
    {
        var informaram = itens.Where(item => item.SituacaoPcd != SituacaoPcd.PrefiroNaoInformar).ToList();
        if (informaram.Count < LimiteSupressao)
        {
            return null;
        }

        var pcd = informaram.Count(item => item.SituacaoPcd == SituacaoPcd.Sim);
        if (pcd is > 0 and < LimiteSupressao)
        {
            return null;
        }

        return Percentual(pcd, informaram.Count);
    }

    private static decimal Percentual(int parte, int total) =>
        total == 0 ? 0 : Math.Round(parte * 100m / total, 1, MidpointRounding.AwayFromZero);

    private static string? LimparDescricao(SituacaoPcd situacao, string? descricao)
    {
        if (situacao != SituacaoPcd.Sim)
        {
            return null;
        }

        var texto = descricao?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static AutodeclaracaoDto ParaDto(Autodeclaracao entidade, DadosAutodeclaracaoProtegidos dados) => new()
    {
        Etnia = dados.Etnia,
        Genero = dados.Genero,
        OrientacaoSexual = dados.OrientacaoSexual,
        TemDeficiencia = dados.SituacaoPcd switch
        {
            SituacaoPcd.Sim => true,
            SituacaoPcd.Nao => false,
            _ => null
        },
        DescricaoDeficiencia = dados.DescricaoDeficiencia,
        ContextoSocioeconomico = dados.ContextoSocioeconomico,
        ConsentimentoLgpd = entidade.ConsentimentoLgpd,
        DataAtualizacao = entidade.DataAtualizacao
    };

    private static AutodeclaracaoResponse ParaResponse(Autodeclaracao entidade, DadosAutodeclaracaoProtegidos dados) => new()
    {
        Id = entidade.Id,
        ColaboradorId = entidade.ColaboradorId,
        Etnia = dados.Etnia,
        Genero = dados.Genero,
        OrientacaoSexual = dados.OrientacaoSexual,
        SituacaoPcd = dados.SituacaoPcd,
        TipoDeficiencia = dados.DescricaoDeficiencia,
        ConsentimentoLgpd = entidade.ConsentimentoLgpd,
        DataConsentimento = entidade.DataConsentimento,
        DataAtualizacao = entidade.DataAtualizacao
    };
}
