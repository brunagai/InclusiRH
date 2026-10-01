namespace InclusiRH.Api.Services.Notificacoes;

public enum ModoEnvioNotificacao
{
    /// <summary>Registro estruturado no log. Padrão seguro para desenvolvimento.</summary>
    Log,

    /// <summary>Grava cada e-mail como arquivo .eml numa pasta local, sem rede.</summary>
    Arquivo,

    /// <summary>Envia por servidor SMTP real.</summary>
    Smtp
}

public sealed class NotificacaoOptions
{
    public const string SectionName = "Notificacoes";

    public ModoEnvioNotificacao Modo { get; set; } = ModoEnvioNotificacao.Log;

    public string Remetente { get; set; } = string.Empty;

    public string NomeRemetente { get; set; } = "InclusiRH Conformidade";

    public List<string> DestinatariosDiretoria { get; set; } = [];

    public List<string> DestinatariosRhSuperior { get; set; } = [];

    /// <summary>
    /// Endereço da fila de denúncias no painel. O e-mail leva só este link,
    /// nunca o conteúdo do relato.
    /// </summary>
    public string UrlPainelDenuncias { get; set; } = string.Empty;

    /// <summary>
    /// Pasta dos .eml no modo Arquivo, relativa à raiz do projeto.
    /// </summary>
    public string DiretorioArquivos { get; set; } = "App_Data/Emails";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Porta { get; set; } = 587;

    public bool UsarSsl { get; set; } = true;

    /// <summary>
    /// Deixe vazio em appsettings.json e informe por variável de ambiente
    /// (Notificacoes__Smtp__Usuario e Notificacoes__Smtp__Senha).
    /// </summary>
    public string? Usuario { get; set; }

    public string? Senha { get; set; }

    public int TimeoutSegundos { get; set; } = 30;
}
