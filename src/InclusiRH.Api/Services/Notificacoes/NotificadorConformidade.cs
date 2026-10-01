using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Notificacoes;

public enum TipoNotificacao
{
    Contestacao,
    PrazoExcedido
}

/// <summary>
/// Notificação formal às instâncias superiores. O corpo nunca leva relato,
/// justificativa, nome da pessoa denunciante nem evidências: só dados do caso
/// e o link do painel, onde o acesso exige login.
/// </summary>
public sealed record NotificacaoConformidade(
    TipoNotificacao Tipo,
    string Protocolo,
    string Assunto,
    string Corpo);

public interface INotificadorConformidade
{
    /// <summary>
    /// Entrega à diretoria e ao RH superior. Lança exceção se a entrega falhar,
    /// para o chamador manter o alerta pendente e tentar de novo depois.
    /// </summary>
    Task<IReadOnlyList<string>> EnviarAsync(NotificacaoConformidade notificacao, CancellationToken cancellationToken);
}

/// <summary>
/// Um único notificador com três modos de entrega (Log, Arquivo, Smtp),
/// escolhidos em appsettings.json. Os modos Arquivo e Smtp usam o mesmo
/// <see cref="MailMessage"/>, então o .eml gravado localmente é exatamente
/// o e-mail que iria pelo servidor.
/// </summary>
public sealed class NotificadorConformidade : INotificadorConformidade
{
    private static readonly EventId EventoNotificacaoFormal = new(7001, "NotificacaoFormalConformidade");

    private readonly NotificacaoOptions _opcoes;
    private readonly string _diretorioArquivos;
    private readonly ILogger<NotificadorConformidade> _logger;

    public NotificadorConformidade(
        IOptions<NotificacaoOptions> opcoes,
        IWebHostEnvironment ambiente,
        ILogger<NotificadorConformidade> logger)
    {
        _opcoes = opcoes.Value;
        _diretorioArquivos = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, _opcoes.DiretorioArquivos));
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> EnviarAsync(
        NotificacaoConformidade notificacao,
        CancellationToken cancellationToken)
    {
        var destinatarios = _opcoes.DestinatariosDiretoria
            .Concat(_opcoes.DestinatariosRhSuperior)
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        switch (_opcoes.Modo)
        {
            case ModoEnvioNotificacao.Arquivo:
                Directory.CreateDirectory(_diretorioArquivos);
                await EnviarPorSmtpClientAsync(notificacao, destinatarios, cliente =>
                {
                    cliente.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    cliente.PickupDirectoryLocation = _diretorioArquivos;
                }, cancellationToken);
                break;

            case ModoEnvioNotificacao.Smtp:
                await EnviarPorSmtpClientAsync(notificacao, destinatarios, cliente =>
                {
                    var smtp = _opcoes.Smtp;
                    cliente.Host = smtp.Host;
                    cliente.Port = smtp.Porta;
                    cliente.EnableSsl = smtp.UsarSsl;
                    cliente.Timeout = smtp.TimeoutSegundos * 1000;
                    cliente.DeliveryMethod = SmtpDeliveryMethod.Network;
                    if (!string.IsNullOrWhiteSpace(smtp.Usuario))
                    {
                        cliente.Credentials = new NetworkCredential(smtp.Usuario, smtp.Senha);
                    }
                }, cancellationToken);
                break;
        }

        // Registro formal em todos os modos. Com EventId fixo e campos nomeados,
        // a auditoria filtra estes eventos em qualquer coletor de logs.
        _logger.Log(
            _opcoes.Modo == ModoEnvioNotificacao.Log ? LogLevel.Warning : LogLevel.Information,
            EventoNotificacaoFormal,
            "Notificação formal {TipoNotificacao} da denúncia {Protocolo} entregue via {ModoEnvio} a {QuantidadeDestinatarios} destinatário(s): {Destinatarios}. Assunto: {Assunto}",
            notificacao.Tipo,
            notificacao.Protocolo,
            _opcoes.Modo,
            destinatarios.Count,
            destinatarios,
            notificacao.Assunto);

        return destinatarios;
    }

    private async Task EnviarPorSmtpClientAsync(
        NotificacaoConformidade notificacao,
        IReadOnlyList<string> destinatarios,
        Action<SmtpClient> configurar,
        CancellationToken cancellationToken)
    {
        using var mensagem = new MailMessage
        {
            From = new MailAddress(_opcoes.Remetente, _opcoes.NomeRemetente),
            Subject = notificacao.Assunto,
            Body = notificacao.Corpo,
            IsBodyHtml = false,
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            Priority = MailPriority.High
        };

        foreach (var destinatario in destinatarios)
        {
            mensagem.To.Add(destinatario);
        }

        mensagem.Headers.Add("X-InclusiRH-Protocolo", notificacao.Protocolo);
        mensagem.Headers.Add("X-InclusiRH-Tipo", notificacao.Tipo.ToString());

        using var cliente = new SmtpClient();
        configurar(cliente);
        await cliente.SendMailAsync(mensagem, cancellationToken);
    }
}
