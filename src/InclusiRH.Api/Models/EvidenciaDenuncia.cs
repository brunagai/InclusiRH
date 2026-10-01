using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

/// <summary>
/// Metadados de um arquivo de evidência. O conteúdo fica fora do banco,
/// cifrado com AES-256-GCM, em <see cref="CaminhoArquivo"/>.
/// </summary>
public sealed class EvidenciaDenuncia
{
    public Guid Id { get; set; }

    public Guid DenunciaId { get; set; }

    /// <summary>
    /// Preenchido quando o arquivo chegou junto com uma contestação:
    /// aponta para a linha do histórico daquela contestação.
    /// </summary>
    public Guid? HistoricoTramitacaoId { get; set; }

    /// <summary>
    /// Nome exibido no download. Em denúncia anônima é um nome genérico,
    /// porque o nome original do arquivo pode identificar quem enviou.
    /// </summary>
    public string NomeArquivo { get; set; } = string.Empty;

    /// <summary>
    /// Caminho relativo à pasta de evidências, no formato {guid}.bin.
    /// Nunca deriva do nome enviado pelo cliente.
    /// </summary>
    public string CaminhoArquivo { get; set; } = string.Empty;

    /// <summary>
    /// Tipo MIME definido pelo servidor a partir da assinatura do arquivo.
    /// </summary>
    public string TipoArquivo { get; set; } = string.Empty;

    public long TamanhoBytes { get; set; }

    public OrigemEvidencia Origem { get; set; }

    public DateTimeOffset DataUpload { get; set; }

    public Denuncia Denuncia { get; set; } = null!;

    public HistoricoTramitacao? HistoricoTramitacao { get; set; }
}
