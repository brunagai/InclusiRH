using System.Security.Cryptography;
using InclusiRH.Api.Data;
using InclusiRH.Api.DTOs.Denuncias;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Evidencias;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Services.Denuncias;

public sealed class DenunciaService : IDenunciaService
{
    private const string AlfabetoProtocolo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static readonly IReadOnlyDictionary<StatusDenuncia, StatusDenuncia[]> TransicoesPermitidas =
        new Dictionary<StatusDenuncia, StatusDenuncia[]>
        {
            [StatusDenuncia.Pendente] = [StatusDenuncia.EmAnalise, StatusDenuncia.Tratado, StatusDenuncia.Escalonado],
            [StatusDenuncia.EmAnalise] = [StatusDenuncia.Tratado, StatusDenuncia.Escalonado],
            [StatusDenuncia.Escalonado] = [StatusDenuncia.EmAnalise, StatusDenuncia.Tratado],
            [StatusDenuncia.Tratado] = [StatusDenuncia.Encerrado, StatusDenuncia.Contestado],
            [StatusDenuncia.Contestado] = [StatusDenuncia.EmAnalise, StatusDenuncia.Escalonado],
            [StatusDenuncia.Encerrado] = []
        };

    private const string ConteudoIndisponivel =
        "[Conteúdo indisponível: a verificação de integridade do registro falhou.]";

    private readonly ApplicationDbContext _db;
    private readonly IProtetorConteudoDenuncia _protetor;
    private readonly IArmazenamentoEvidencias _armazenamento;
    private readonly ILogger<DenunciaService> _logger;

    public DenunciaService(
        ApplicationDbContext db,
        IProtetorConteudoDenuncia protetor,
        IArmazenamentoEvidencias armazenamento,
        ILogger<DenunciaService> logger)
    {
        _db = db;
        _protetor = protetor;
        _armazenamento = armazenamento;
        _logger = logger;
    }

    public async Task<DenunciaResponse> AbrirAsync(
        Guid colaboradorId,
        CriarDenunciaDto dto,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(dto.TipoOcorrencia, "tipo de ocorrência");

        var descricao = dto.Descricao.Trim();
        if (descricao.Length == 0)
        {
            throw new RegraDeNegocioException("A descrição da denúncia é obrigatória.");
        }

        // Todos os arquivos são validados antes de qualquer gravação.
        var arquivos = await ValidadorEvidencias.ValidarAsync(dto.Evidencias, cancellationToken);

        var colaboradorAtivo = await _db.Colaboradores
            .AnyAsync(c => c.Id == colaboradorId && c.Ativo, cancellationToken);

        if (!colaboradorAtivo)
        {
            throw new RegraDeNegocioException("Colaborador não encontrado ou inativo.");
        }

        // O vínculo fica guardado para a pessoa validar a tratativa.
        // IsAnonima só impede que RH e diretoria vejam quem abriu.
        // O relato é cifrado aqui, antes de chegar ao EF: o texto legível
        // nunca vai para o banco nem para o log de comandos SQL.
        var id = Guid.NewGuid();
        var denuncia = new Denuncia
        {
            Id = id,
            TipoOcorrencia = dto.TipoOcorrencia,
            DescricaoProtegida = _protetor.Proteger(id, CampoDenunciaProtegido.Descricao, descricao),
            IsAnonima = dto.IsAnonima,
            Status = StatusDenuncia.Pendente,
            DataCriacao = DateTimeOffset.UtcNow,
            ColaboradorId = colaboradorId,
            Protocolo = GerarProtocolo()
        };

        var gravados = await GravarEvidenciasAsync(denuncia, arquivos, OrigemEvidencia.Abertura, historico: null, cancellationToken);
        try
        {
            for (var tentativa = 1; tentativa <= 3; tentativa++)
            {
                _db.Denuncias.Add(denuncia);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    return Mapear(denuncia, exporIdentidade: true, incluirHistorico: false);
                }
                catch (DbUpdateException ex) when (EhViolacaoDeUnicidade(ex))
                {
                    DesanexarDoContexto(denuncia);
                    if (tentativa == 3)
                    {
                        break;
                    }

                    denuncia.Protocolo = GerarProtocolo();
                }
            }

            throw new RegraDeNegocioException("Não foi possível gerar um protocolo único. Tente novamente.");
        }
        catch
        {
            // Sem registro no banco, o arquivo cifrado em disco ficaria órfão.
            RemoverArquivos(gravados);
            throw;
        }
    }

    public async Task<IReadOnlyList<DenunciaResponse>> ListarAsync(
        Guid solicitanteId,
        bool podeVerTodas,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);

        var consulta = _db.Denuncias
            .AsNoTracking()
            .Include(d => d.Colaborador)
            .Include(d => d.Evidencias)
            .AsSplitQuery()
            .AsQueryable();

        if (!podeVerTodas)
        {
            consulta = consulta.Where(d => d.ColaboradorId == solicitanteId);
        }

        var denuncias = await consulta
            .OrderByDescending(d => d.DataCriacao)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);

        return denuncias
            .Select(d => Mapear(d, ExporIdentidade(d, solicitanteId, podeVerTodas), incluirHistorico: false))
            .ToList();
    }

    public async Task<DenunciaResponse?> ObterPorProtocoloAsync(
        string protocolo,
        Guid solicitanteId,
        bool podeVerTodas,
        CancellationToken cancellationToken)
    {
        var denuncia = await _db.Denuncias
            .AsNoTracking()
            .Include(d => d.Colaborador)
            .Include(d => d.Historicos)
            .Include(d => d.Evidencias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Protocolo == NormalizarProtocolo(protocolo), cancellationToken);

        if (denuncia is null || !PodeConsultar(denuncia, solicitanteId, podeVerTodas))
        {
            return null;
        }

        return Mapear(denuncia, ExporIdentidade(denuncia, solicitanteId, podeVerTodas), incluirHistorico: true);
    }

    public async Task<DenunciaResponse?> RegistrarTratativaAsync(
        Guid denunciaId,
        string autor,
        RegistrarTratativaDto dto,
        CancellationToken cancellationToken)
    {
        var parecer = dto.ParecerRH.Trim();
        if (parecer.Length == 0)
        {
            throw new RegraDeNegocioException("O parecer do RH é obrigatório.");
        }

        var denuncia = await _db.Denuncias
            .Include(d => d.Colaborador)
            .Include(d => d.Historicos)
            .Include(d => d.Evidencias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == denunciaId, cancellationToken);

        if (denuncia is null)
        {
            return null;
        }

        ValidarTransicao(denuncia.Status, StatusDenuncia.Tratado);
        RegistrarHistorico(denuncia, parecer, dto.EvidenciasAnexadas, autor, StatusDenuncia.Tratado);
        denuncia.Status = StatusDenuncia.Tratado;
        await _db.SaveChangesAsync(cancellationToken);

        return Mapear(denuncia, exporIdentidade: !denuncia.IsAnonima, incluirHistorico: true);
    }

    public async Task<ValidacaoDenunciaResponse?> ValidarAsync(
        Guid denunciaId,
        Guid colaboradorId,
        ValidarDenunciaDto dto,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(dto.StatusValidacao, "status de validação");

        var denuncia = await _db.Denuncias
            .Include(d => d.Colaborador)
            .Include(d => d.Historicos)
            .Include(d => d.Evidencias)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.Id == denunciaId, cancellationToken);

        if (denuncia is null || denuncia.ColaboradorId != colaboradorId)
        {
            return null;
        }

        var novoStatus = dto.StatusValidacao == StatusValidacao.Confirmado
            ? StatusDenuncia.Encerrado
            : StatusDenuncia.Contestado;

        ValidarTransicao(denuncia.Status, novoStatus);

        string? justificativa = null;
        if (dto.StatusValidacao == StatusValidacao.Contestado)
        {
            justificativa = dto.JustificativaContestacao?.Trim();
            if (string.IsNullOrEmpty(justificativa))
            {
                throw new RegraDeNegocioException("A contestação exige uma justificativa.");
            }
        }
        else if (dto.Evidencias is { Count: > 0 })
        {
            throw new RegraDeNegocioException("Novas evidências só podem ser enviadas ao contestar a tratativa.");
        }

        // Arquivos só são lidos depois de confirmar autoria e transição válida,
        // para quem não pode validar o caso não conseguir ocupar memória e disco.
        var arquivos = await ValidadorEvidencias.ValidarAsync(dto.Evidencias, cancellationToken);

        var autorHistorico = denuncia.IsAnonima ? "Denunciante" : denuncia.Colaborador?.Nome ?? "Denunciante";
        // O histórico é texto aberto. A justificativa fica só no campo cifrado
        // da denúncia; aqui entra apenas o registro de que houve contestação.
        var parecer = novoStatus == StatusDenuncia.Encerrado
            ? "Tratativa confirmada pela pessoa denunciante. Caso encerrado."
            : "Tratativa contestada pela pessoa denunciante. A justificativa está protegida no caso.";

        var historicoValidacao = RegistrarHistorico(denuncia, parecer, evidencias: null, autorHistorico, novoStatus);

        var contestada = novoStatus == StatusDenuncia.Contestado;
        if (contestada)
        {
            // A notificação formal sai pelo AlertaEscalonamentoWorker. Data nula
            // coloca o caso na fila; uma nova contestação do mesmo caso também
            // volta para a fila, mesmo que a anterior já tenha sido notificada.
            denuncia.DataAlertaContestacao = null;
            RegistrarHistorico(
                denuncia,
                "Contestação registrada. Escalonamento para a diretoria e o RH superior em processamento.",
                evidencias: null,
                "Sistema de conformidade",
                StatusDenuncia.Contestado);
        }

        denuncia.Status = novoStatus;
        denuncia.JustificativaContestacaoProtegida = justificativa is null
            ? null
            : _protetor.Proteger(denuncia.Id, CampoDenunciaProtegido.JustificativaContestacao, justificativa);
        denuncia.DataValidacao = DateTimeOffset.UtcNow;

        // As novas evidências ficam ligadas à linha do histórico desta contestação.
        var gravados = await GravarEvidenciasAsync(
            denuncia, arquivos, OrigemEvidencia.Contestacao, historicoValidacao, cancellationToken);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            RemoverArquivos(gravados);
            throw;
        }

        return new ValidacaoDenunciaResponse
        {
            Denuncia = Mapear(denuncia, exporIdentidade: true, incluirHistorico: true),
            AlertaEscalonamentoDisparado = contestada,
            MensagemAlerta = contestada
                ? "Contestação registrada. A diretoria e o RH superior serão notificados formalmente."
                : null
        };
    }

    public async Task<HistoricoTramitacaoResponse?> RegistrarTramitacaoAsync(
        string protocolo,
        RegistrarTramitacaoRequest request,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(request.Status, "status");

        var parecer = request.ParecerRH.Trim();
        var autor = request.Autor.Trim();
        if (parecer.Length == 0 || autor.Length == 0)
        {
            throw new RegraDeNegocioException("Parecer e autor são obrigatórios.");
        }

        var denuncia = await _db.Denuncias
            .FirstOrDefaultAsync(d => d.Protocolo == NormalizarProtocolo(protocolo), cancellationToken);

        if (denuncia is null)
        {
            return null;
        }

        ValidarTransicao(denuncia.Status, request.Status);
        var historico = RegistrarHistorico(denuncia, parecer, request.EvidenciasAnexadas, autor, request.Status);
        denuncia.Status = request.Status;
        await _db.SaveChangesAsync(cancellationToken);
        return Mapear(historico);
    }

    public async Task<ArquivoEvidenciaDownload?> ObterEvidenciaAsync(
        string protocolo,
        Guid evidenciaId,
        Guid solicitanteId,
        bool podeVerTodas,
        CancellationToken cancellationToken)
    {
        var evidencia = await _db.EvidenciasDenuncia
            .AsNoTracking()
            .Include(e => e.Denuncia)
            .FirstOrDefaultAsync(
                e => e.Id == evidenciaId && e.Denuncia.Protocolo == NormalizarProtocolo(protocolo),
                cancellationToken);

        // Mesma regra de leitura da denúncia: autora do caso ou RH/Admin.
        // Sem acesso, a resposta é igual à de arquivo inexistente.
        if (evidencia is null || !PodeConsultar(evidencia.Denuncia, solicitanteId, podeVerTodas))
        {
            return null;
        }

        try
        {
            var conteudo = await _armazenamento.LerAsync(evidencia.Id, evidencia.CaminhoArquivo, cancellationToken);
            return new ArquivoEvidenciaDownload(evidencia.NomeArquivo, evidencia.TipoArquivo, conteudo);
        }
        catch (Exception ex) when (ex is CryptographicException or FileNotFoundException)
        {
            _logger.LogError("Evidência {EvidenciaId} ausente ou com falha de integridade.", evidencia.Id);
            throw new RegraDeNegocioException("Arquivo indisponível: a verificação de integridade falhou.");
        }
    }

    /// <summary>
    /// Cifra e grava cada arquivo em disco e anexa os metadados à denúncia.
    /// Se um arquivo falhar no meio, os já gravados são apagados.
    /// Retorna os caminhos gravados para limpeza caso o banco recuse depois.
    /// </summary>
    private async Task<IReadOnlyList<string>> GravarEvidenciasAsync(
        Denuncia denuncia,
        IReadOnlyList<ArquivoEvidenciaValidado> arquivos,
        OrigemEvidencia origem,
        HistoricoTramitacao? historico,
        CancellationToken cancellationToken)
    {
        var gravados = new List<string>(arquivos.Count);
        try
        {
            var sequencia = denuncia.Evidencias.Count;
            foreach (var arquivo in arquivos)
            {
                sequencia++;
                var evidenciaId = Guid.NewGuid();
                var caminho = await _armazenamento.SalvarAsync(evidenciaId, arquivo.Conteudo, cancellationToken);
                gravados.Add(caminho);

                var evidencia = new EvidenciaDenuncia
                {
                    Id = evidenciaId,
                    DenunciaId = denuncia.Id,
                    HistoricoTramitacaoId = historico?.Id,
                    // O nome original ("print_conversa_joao.png") pode identificar
                    // quem denunciou. Em caso anônimo, o RH vê só um nome genérico.
                    NomeArquivo = denuncia.IsAnonima ? $"evidencia-{sequencia}{arquivo.Extensao}" : arquivo.NomeOriginal,
                    CaminhoArquivo = caminho,
                    TipoArquivo = arquivo.TipoArquivo,
                    TamanhoBytes = arquivo.Conteudo.LongLength,
                    Origem = origem,
                    DataUpload = DateTimeOffset.UtcNow
                };

                denuncia.Evidencias.Add(evidencia);
                if (_db.Entry(denuncia).State != EntityState.Detached)
                {
                    _db.EvidenciasDenuncia.Add(evidencia);
                }
            }

            return gravados;
        }
        catch
        {
            RemoverArquivos(gravados);
            throw;
        }
    }

    private void RemoverArquivos(IEnumerable<string> caminhos)
    {
        foreach (var caminho in caminhos)
        {
            try
            {
                _armazenamento.Remover(caminho);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Não foi possível remover o arquivo de evidência órfão {Caminho}.", caminho);
            }
        }
    }

    /// <summary>
    /// Tira a denúncia e as evidências do rastreamento do EF para tentar de
    /// novo com outro protocolo, sem gravar nada duplicado.
    /// </summary>
    private void DesanexarDoContexto(Denuncia denuncia)
    {
        foreach (var evidencia in denuncia.Evidencias)
        {
            _db.Entry(evidencia).State = EntityState.Detached;
        }

        _db.Entry(denuncia).State = EntityState.Detached;
    }

    private static bool PodeConsultar(Denuncia denuncia, Guid solicitanteId, bool podeVerTodas) =>
        podeVerTodas || denuncia.ColaboradorId == solicitanteId;

    private static bool ExporIdentidade(Denuncia denuncia, Guid solicitanteId, bool podeVerTodas)
    {
        if (denuncia.ColaboradorId == solicitanteId)
        {
            return true;
        }

        return podeVerTodas && !denuncia.IsAnonima;
    }

    private HistoricoTramitacao RegistrarHistorico(
        Denuncia denuncia,
        string parecer,
        string? evidencias,
        string autor,
        StatusDenuncia status)
    {
        var historico = new HistoricoTramitacao
        {
            Id = Guid.NewGuid(),
            DenunciaId = denuncia.Id,
            ParecerRH = parecer,
            EvidenciasAnexadas = LimparTexto(evidencias),
            DataTramitacao = DateTimeOffset.UtcNow,
            Autor = autor,
            StatusResultante = status
        };

        // Com a chave já preenchida, o EF trataria um item incluído só pela
        // navegação como registro existente e geraria UPDATE em vez de INSERT.
        // Add no contexto também inclui o item em denuncia.Historicos quando a
        // denúncia é rastreada; por isso a inclusão manual vem antes e só uma vez.
        denuncia.Historicos.Add(historico);
        _db.HistoricosTramitacao.Add(historico);
        return historico;
    }

    private static void ValidarTransicao(StatusDenuncia atual, StatusDenuncia novo)
    {
        if (atual == novo)
        {
            return;
        }

        if (!TransicoesPermitidas.TryGetValue(atual, out var destinos) || !destinos.Contains(novo))
        {
            throw new RegraDeNegocioException(
                $"Não é permitido alterar o status de {atual} para {novo}.");
        }
    }

    private static string GerarProtocolo()
    {
        Span<char> token = stackalloc char[8];
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);

        for (var i = 0; i < token.Length; i++)
        {
            token[i] = AlfabetoProtocolo[bytes[i] % AlfabetoProtocolo.Length];
        }

        return $"INC-{DateTime.UtcNow:yyyyMMdd}-{new string(token)}";
    }

    private static string NormalizarProtocolo(string protocolo) => protocolo.Trim().ToUpperInvariant();

    private static string? LimparTexto(string? valor)
    {
        var texto = valor?.Trim();
        return string.IsNullOrEmpty(texto) ? null : texto;
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;

    /// <summary>
    /// Único ponto que decifra o conteúdo. Todos os chamadores já filtraram o
    /// acesso antes: autoria do caso (colaborador) ou política RH/Admin, seja na
    /// consulta (ListarAsync, PodeConsultar) seja na rota ([Authorize(Policy)]).
    /// </summary>
    private DenunciaResponse Mapear(Denuncia denuncia, bool exporIdentidade, bool incluirHistorico) => new()
    {
        Id = denuncia.Id,
        Protocolo = denuncia.Protocolo,
        TipoOcorrencia = denuncia.TipoOcorrencia,
        Descricao = Revelar(denuncia.Id, CampoDenunciaProtegido.Descricao, denuncia.DescricaoProtegida),
        IsAnonima = denuncia.IsAnonima,
        Status = denuncia.Status,
        DataCriacao = denuncia.DataCriacao,
        ColaboradorId = exporIdentidade ? denuncia.ColaboradorId : null,
        NomeColaborador = exporIdentidade ? denuncia.Colaborador?.Nome : null,
        JustificativaContestacao = denuncia.JustificativaContestacaoProtegida is null
            ? null
            : Revelar(denuncia.Id, CampoDenunciaProtegido.JustificativaContestacao, denuncia.JustificativaContestacaoProtegida),
        DataValidacao = denuncia.DataValidacao,
        Historicos = incluirHistorico
            ? denuncia.Historicos.OrderBy(h => h.DataTramitacao).Select(Mapear).ToList()
            : [],
        Evidencias = denuncia.Evidencias
            .OrderBy(e => e.DataUpload)
            .Select(e => new EvidenciaResponse
            {
                Id = e.Id,
                NomeArquivo = e.NomeArquivo,
                TipoArquivo = e.TipoArquivo,
                TamanhoBytes = e.TamanhoBytes,
                Origem = e.Origem,
                DataUpload = e.DataUpload,
                HistoricoTramitacaoId = e.HistoricoTramitacaoId,
                Url = $"/api/denuncias/{denuncia.Protocolo}/evidencias/{e.Id}"
            })
            .ToList()
    };

    /// <summary>
    /// Um registro adulterado ou cifrado com outra chave não derruba a fila
    /// inteira: o campo sai marcado como indisponível e o incidente vai para
    /// o log só com o Id, sem conteúdo.
    /// </summary>
    private string Revelar(Guid denunciaId, CampoDenunciaProtegido campo, byte[] pacote)
    {
        try
        {
            return _protetor.Revelar(denunciaId, campo, pacote);
        }
        catch (CryptographicException)
        {
            _logger.LogError(
                "Falha de integridade ao decifrar {Campo} da denúncia {DenunciaId}.",
                campo,
                denunciaId);
            return ConteudoIndisponivel;
        }
    }

    private static HistoricoTramitacaoResponse Mapear(HistoricoTramitacao historico) => new()
    {
        Id = historico.Id,
        ParecerRH = historico.ParecerRH,
        EvidenciasAnexadas = historico.EvidenciasAnexadas,
        DataTramitacao = historico.DataTramitacao,
        Autor = historico.Autor,
        Status = historico.StatusResultante
    };
}
