using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Models;

public sealed class HistoricoTramitacao
{
    public Guid Id { get; set; }

    public Guid DenunciaId { get; set; }

    public string ParecerRH { get; set; } = string.Empty;

    public string? EvidenciasAnexadas { get; set; }

    public DateTimeOffset DataTramitacao { get; set; }

    public string Autor { get; set; } = string.Empty;

    public StatusDenuncia StatusResultante { get; set; }

    public Denuncia Denuncia { get; set; } = null!;

    public ICollection<EvidenciaDenuncia> Evidencias { get; set; } = new List<EvidenciaDenuncia>();
}
