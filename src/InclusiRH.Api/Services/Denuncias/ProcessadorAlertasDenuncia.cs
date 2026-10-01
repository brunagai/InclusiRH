using System.Linq.Expressions;
using InclusiRH.Api.Data;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Background;
using InclusiRH.Api.Services.Notificacoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Denuncias;

public sealed record ResultadoProcessamentoAlertas(int Enviados, int Falhas);

public interface IProcessadorAlertasDenuncia
{
    Task<ResultadoProcessamentoAlertas> ProcessarContestacoesAsync(CancellationToken cancellationToken);

    Task<ResultadoProcessamentoAlertas> ProcessarPrazosExcedidosAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Regras de negócio dos alertas formais, chamadas pelo worker.
///
/// Garantia de entrega (fila persistente no próprio banco):
/// - O alerta pendente é o estado do caso: Contestado com DataAlertaContestacao
///   nula, ou Pendente/EmAnalise vencido com DataAlertaPrazo nula.
/// - Primeiro a notificação é enviada; só depois a data é gravada junto com a
///   linha de auditoria no histórico. Se o envio falhar, nada é gravado e o
///   caso volta na próxima varredura.
/// - Com a data gravada, o caso sai da consulta: não há reenvio em loop.
/// - Se a API cair entre o envio e a gravação, o alerta sai de novo na próxima
///   varredura. Um aviso repetido é preferível a um aviso perdido.
/// </summary>
public sealed class ProcessadorAlertasDenuncia : IProcessadorAlertasDenuncia
{
    private const string AutorSistema = "Sistema de conformidade";

    private readonly ApplicationDbContext _db;
    private readonly INotificadorConformidade _notificador;
    private readonly AlertaEscalonamentoOptions _opcoes;
    private readonly NotificacaoOptions _notificacao;
    private readonly ILogger<ProcessadorAlertasDenuncia> _logger;

    public ProcessadorAlertasDenuncia(
        ApplicationDbContext db,
        INotificadorConformidade notificador,
        IOptions<AlertaEscalonamentoOptions> opcoes,
        IOptions<NotificacaoOptions> notificacao,
        ILogger<ProcessadorAlertasDenuncia> logger)
    {
        _db = db;
        _notificador = notificador;
        _opcoes = opcoes.Value;
        _notificacao = notificacao.Value;
        _logger = logger;
    }

    public async Task<ResultadoProcessamentoAlertas> ProcessarContestacoesAsync(CancellationToken cancellationToken)
    {
        Expression<Func<Denuncia, bool>> pendente =
            d => d.Status == StatusDenuncia.Contestado && d.DataAlertaContestacao == null;

        var ids = await _db.Denuncias
            .Where(pendente)
            .OrderBy(d => d.DataValidacao)
            .Select(d => d.Id)
            .Take(_opcoes.LoteMaximo)
            .ToListAsync(cancellationToken);

        return await ProcessarLoteAsync(ids, pendente, TipoNotificacao.Contestacao, async (denuncia, ct) =>
        {
            var destinatarios = await _notificador.EnviarAsync(MontarNotificacaoContestacao(denuncia), ct);

            denuncia.DataAlertaContestacao = DateTimeOffset.UtcNow;
            RegistrarAuditoria(
                denuncia,
                $"Notificação formal de contestação enviada à diretoria e ao RH superior ({destinatarios.Count} destinatário(s)). Caso aguarda intervenção superior.",
                denuncia.Status);
        }, cancellationToken);
    }

    public async Task<ResultadoProcessamentoAlertas> ProcessarPrazosExcedidosAsync(CancellationToken cancellationToken)
    {
        var limite = DateTimeOffset.UtcNow.AddDays(-_opcoes.PrazoTratativaDias);
        Expression<Func<Denuncia, bool>> vencida =
            d => (d.Status == StatusDenuncia.Pendente || d.Status == StatusDenuncia.EmAnalise)
                && d.DataCriacao <= limite
                && d.DataAlertaPrazo == null;

        var ids = await _db.Denuncias
            .Where(vencida)
            .OrderBy(d => d.DataCriacao)
            .Select(d => d.Id)
            .Take(_opcoes.LoteMaximo)
            .ToListAsync(cancellationToken);

        return await ProcessarLoteAsync(ids, vencida, TipoNotificacao.PrazoExcedido, async (denuncia, ct) =>
        {
            var statusAnterior = denuncia.Status;
            var destinatarios = await _notificador.EnviarAsync(MontarNotificacaoPrazo(denuncia), ct);

            // Pendente e EmAnalise podem ir para Escalonado na máquina de estados.
            // O caso passa a aparecer no painel de escalonamento da diretoria.
            denuncia.Status = StatusDenuncia.Escalonado;
            denuncia.DataAlertaPrazo = DateTimeOffset.UtcNow;
            RegistrarAuditoria(
                denuncia,
                $"Prazo regulamentar de {_opcoes.PrazoTratativaDias} dia(s) excedido em {statusAnterior}. Caso escalonado automaticamente e notificação formal enviada à diretoria e ao RH superior ({destinatarios.Count} destinatário(s)).",
                StatusDenuncia.Escalonado);
        }, cancellationToken);
    }

    /// <summary>
    /// Cada caso é independente: a falha de um (SMTP fora, conflito de edição)
    /// é registrada e não impede os demais.
    ///
    /// Cada caso é recarregado com o contexto limpo, conferindo de novo se o
    /// alerta ainda está pendente. Assim o descarte das alterações de um caso
    /// com falha não afeta os seguintes, e um caso tratado pelo RH depois da
    /// consulta dos Ids não gera alerta.
    /// </summary>
    private async Task<ResultadoProcessamentoAlertas> ProcessarLoteAsync(
        IReadOnlyList<Guid> ids,
        Expression<Func<Denuncia, bool>> aindaPendente,
        TipoNotificacao tipo,
        Func<Denuncia, CancellationToken, Task> processar,
        CancellationToken cancellationToken)
    {
        var enviados = 0;
        var falhas = 0;

        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _db.ChangeTracker.Clear();

            Denuncia? denuncia = null;
            try
            {
                denuncia = await _db.Denuncias
                    .Where(d => d.Id == id)
                    .Where(aindaPendente)
                    .SingleOrDefaultAsync(cancellationToken);
                if (denuncia is null)
                {
                    continue;
                }

                await processar(denuncia, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                enviados++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException)
            {
                // O RH alterou o caso durante o envio. A notificação já saiu;
                // a próxima varredura reavalia o caso com o estado atual.
                falhas++;
                _logger.LogWarning(
                    "Denúncia {Protocolo} alterada durante o alerta {TipoNotificacao}; será reavaliada na próxima varredura.",
                    denuncia?.Protocolo,
                    tipo);
            }
            catch (Exception ex)
            {
                falhas++;
                _logger.LogError(
                    ex,
                    "Falha no alerta {TipoNotificacao} da denúncia {DenunciaId}; permanece pendente para nova tentativa.",
                    tipo,
                    id);
            }
        }

        _db.ChangeTracker.Clear();
        return new ResultadoProcessamentoAlertas(enviados, falhas);
    }

    private void RegistrarAuditoria(Denuncia denuncia, string texto, StatusDenuncia status)
    {
        _db.HistoricosTramitacao.Add(new HistoricoTramitacao
        {
            Id = Guid.NewGuid(),
            DenunciaId = denuncia.Id,
            ParecerRH = texto,
            DataTramitacao = DateTimeOffset.UtcNow,
            Autor = AutorSistema,
            StatusResultante = status
        });
    }

    private NotificacaoConformidade MontarNotificacaoContestacao(Denuncia denuncia) => new(
        TipoNotificacao.Contestacao,
        denuncia.Protocolo,
        $"[InclusiRH] Denúncia {denuncia.Protocolo} contestada: intervenção da diretoria necessária",
        MontarCorpo(
            denuncia,
            "A pessoa denunciante contestou a tratativa registrada pelo RH. Conforme a política de validação bilateral, o caso sai do escopo imediato do RH e passa à diretoria e ao RH superior.",
            $"Contestada em: {Formatar(denuncia.DataValidacao)}"));

    private NotificacaoConformidade MontarNotificacaoPrazo(Denuncia denuncia) => new(
        TipoNotificacao.PrazoExcedido,
        denuncia.Protocolo,
        $"[InclusiRH] Denúncia {denuncia.Protocolo} sem tratativa há mais de {_opcoes.PrazoTratativaDias} dias",
        MontarCorpo(
            denuncia,
            $"O caso ultrapassou o prazo regulamentar de {_opcoes.PrazoTratativaDias} dia(s) sem tratativa registrada e foi escalonado automaticamente.",
            $"Status antes do escalonamento: {denuncia.Status}"));

    /// <summary>
    /// Só metadados. Relato, justificativa, evidências e identidade ficam no
    /// sistema, cifrados, e só são vistos no painel por quem tem permissão.
    /// </summary>
    private string MontarCorpo(Denuncia denuncia, string motivo, string linhaExtra)
    {
        var painel = string.IsNullOrWhiteSpace(_notificacao.UrlPainelDenuncias)
            ? "painel administrativo do InclusiRH"
            : $"{_notificacao.UrlPainelDenuncias.TrimEnd('/')}/{denuncia.Protocolo}";

        return $"""
            Notificação formal de conformidade — InclusiRH

            {motivo}

            Protocolo: {denuncia.Protocolo}
            Tipo de ocorrência: {denuncia.TipoOcorrencia}
            Aberta em: {Formatar(denuncia.DataCriacao)}
            {linhaExtra}
            Denúncia anônima: {(denuncia.IsAnonima ? "sim" : "não")}

            Por sigilo e em conformidade com a LGPD, o relato, a justificativa, as evidências
            e a identidade da pessoa denunciante não são enviados por e-mail.
            Consulte o caso em: {painel}

            Mensagem automática. Não responda a este e-mail.
            """;
    }

    private static string Formatar(DateTimeOffset? data) =>
        data is null
            ? "não informado"
            : $"{data.Value.ToOffset(TimeSpan.FromHours(-3)):dd/MM/yyyy HH:mm} (horário de Brasília)";
}
