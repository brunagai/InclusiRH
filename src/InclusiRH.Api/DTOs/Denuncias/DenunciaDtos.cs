using System.ComponentModel.DataAnnotations;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.DTOs.Denuncias;

/// <summary>
/// Enviado como multipart/form-data. Os arquivos vão no campo "evidencias",
/// que pode se repetir (até 5 arquivos, 10 MB cada).
/// </summary>
public sealed class CriarDenunciaDto
{
    [Required]
    public TipoOcorrencia TipoOcorrencia { get; set; }

    [Required(ErrorMessage = "Descreva a ocorrência.")]
    [MaxLength(4000)]
    public string Descricao { get; set; } = string.Empty;

    public bool IsAnonima { get; set; }

    /// <summary>
    /// Opcional. PDF, PNG, JPG, MP3, M4A, WAV ou OGG.
    /// </summary>
    public List<IFormFile>? Evidencias { get; set; }
}

public sealed class RegistrarTratativaDto
{
    [Required(ErrorMessage = "Informe o parecer do RH.")]
    [MaxLength(4000)]
    public string ParecerRH { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? EvidenciasAnexadas { get; set; }
}

/// <summary>
/// Enviado como multipart/form-data. Novas evidências só são aceitas
/// quando o status é Contestado.
/// </summary>
public sealed class ValidarDenunciaDto
{
    [Required(ErrorMessage = "Informe se a tratativa foi confirmada ou contestada.")]
    public StatusValidacao StatusValidacao { get; set; }

    [MaxLength(4000)]
    public string? JustificativaContestacao { get; set; }

    public List<IFormFile>? Evidencias { get; set; }
}

public sealed class EvidenciaResponse
{
    public Guid Id { get; init; }

    public string NomeArquivo { get; init; } = string.Empty;

    public string TipoArquivo { get; init; } = string.Empty;

    public long TamanhoBytes { get; init; }

    public OrigemEvidencia Origem { get; init; }

    public DateTimeOffset DataUpload { get; init; }

    /// <summary>
    /// Linha do histórico da contestação que trouxe o arquivo, quando houver.
    /// </summary>
    public Guid? HistoricoTramitacaoId { get; init; }

    /// <summary>
    /// Rota de download. Exige o mesmo acesso que a denúncia.
    /// </summary>
    public string Url { get; init; } = string.Empty;
}

/// <summary>
/// Conteúdo decifrado para o download.
/// </summary>
public sealed record ArquivoEvidenciaDownload(string NomeArquivo, string TipoArquivo, byte[] Conteudo);

public sealed class RegistrarTramitacaoRequest
{
    [Required(ErrorMessage = "Informe o parecer.")]
    [MaxLength(4000)]
    public string ParecerRH { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? EvidenciasAnexadas { get; set; }

    [Required(ErrorMessage = "Informe o autor da tramitação.")]
    [MaxLength(200)]
    public string Autor { get; set; } = string.Empty;

    [Required]
    public StatusDenuncia Status { get; set; }
}

public sealed class HistoricoTramitacaoResponse
{
    public Guid Id { get; init; }

    public string ParecerRH { get; init; } = string.Empty;

    public string? EvidenciasAnexadas { get; init; }

    public DateTimeOffset DataTramitacao { get; init; }

    public string Autor { get; init; } = string.Empty;

    public StatusDenuncia Status { get; init; }
}

public sealed class DenunciaResponse
{
    public Guid Id { get; init; }

    public string Protocolo { get; init; } = string.Empty;

    public TipoOcorrencia TipoOcorrencia { get; init; }

    public string Descricao { get; init; } = string.Empty;

    public bool IsAnonima { get; init; }

    public StatusDenuncia Status { get; init; }

    public DateTimeOffset DataCriacao { get; init; }

    public Guid? ColaboradorId { get; init; }

    public string? NomeColaborador { get; init; }

    public string? JustificativaContestacao { get; init; }

    public DateTimeOffset? DataValidacao { get; init; }

    public IReadOnlyList<HistoricoTramitacaoResponse> Historicos { get; init; } = [];

    public IReadOnlyList<EvidenciaResponse> Evidencias { get; init; } = [];
}

public sealed class ValidacaoDenunciaResponse
{
    public DenunciaResponse Denuncia { get; init; } = new();

    public bool AlertaEscalonamentoDisparado { get; init; }

    public string? MensagemAlerta { get; init; }
}
