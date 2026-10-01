using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

public sealed class Denuncia
{
    public Guid Id { get; set; }

    public string Protocolo { get; set; } = string.Empty;

    public TipoOcorrencia TipoOcorrencia { get; set; }

    /// <summary>
    /// Relato cifrado com AES-256-GCM, vinculado ao <see cref="Id"/>.
    /// O texto legível só existe na memória da API, para quem pode ver o caso.
    /// </summary>
    public byte[] DescricaoProtegida { get; set; } = [];

    public bool IsAnonima { get; set; }

    public StatusDenuncia Status { get; set; } = StatusDenuncia.Pendente;

    public DateTimeOffset DataCriacao { get; set; }

    /// <summary>
    /// Sempre preenchido para a validação bilateral. Quando <see cref="IsAnonima"/>
    /// é verdadeiro, a identidade não sai nas respostas destinadas ao RH.
    /// </summary>
    public Guid? ColaboradorId { get; set; }

    /// <summary>
    /// Justificativa da contestação, cifrada da mesma forma que o relato.
    /// </summary>
    public byte[]? JustificativaContestacaoProtegida { get; set; }

    public DateTimeOffset? DataValidacao { get; set; }

    /// <summary>
    /// Quando a notificação formal da contestação saiu para diretoria e RH superior.
    /// Nulo com status Contestado significa alerta pendente na fila do worker.
    /// Uma nova contestação volta o campo para nulo.
    /// </summary>
    public DateTimeOffset? DataAlertaContestacao { get; set; }

    /// <summary>
    /// Quando o caso foi escalonado e notificado por estourar o prazo de tratativa.
    /// Preenchido uma única vez por denúncia.
    /// </summary>
    public DateTimeOffset? DataAlertaPrazo { get; set; }

    /// <summary>
    /// Versão da linha (rowversion). Se o RH e o worker alterarem o mesmo caso
    /// ao mesmo tempo, a segunda gravação é recusada em vez de sobrescrever a primeira.
    /// </summary>
    public byte[] VersaoLinha { get; set; } = [];

    public Colaborador? Colaborador { get; set; }

    public ICollection<HistoricoTramitacao> Historicos { get; set; } = new List<HistoricoTramitacao>();

    public ICollection<EvidenciaDenuncia> Evidencias { get; set; } = new List<EvidenciaDenuncia>();
}
