namespace InclusiRH.Api.Services.Background;

public sealed class AlertaEscalonamentoOptions
{
    public const string SectionName = "BackgroundServices:AlertaEscalonamento";

    public bool Habilitado { get; set; } = true;

    /// <summary>
    /// Espera entre duas varreduras do banco.
    /// </summary>
    public int IntervaloMinutos { get; set; } = 5;

    /// <summary>
    /// Espera antes da primeira varredura, para a API terminar de subir.
    /// </summary>
    public int AtrasoInicialSegundos { get; set; } = 15;

    /// <summary>
    /// Prazo regulamentar de tratativa, contado da abertura da denúncia.
    /// Passado esse prazo em Pendente ou EmAnalise, o caso é escalonado.
    /// </summary>
    public int PrazoTratativaDias { get; set; } = 15;

    /// <summary>
    /// Máximo de casos por tipo de alerta em cada varredura. O restante
    /// fica para a próxima, sem segurar a conexão com o banco por muito tempo.
    /// </summary>
    public int LoteMaximo { get; set; } = 50;
}
