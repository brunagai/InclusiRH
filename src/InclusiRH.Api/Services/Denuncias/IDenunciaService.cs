using InclusiRH.Api.DTOs.Denuncias;

namespace InclusiRH.Api.Services.Denuncias;

public interface IDenunciaService
{
    Task<DenunciaResponse> AbrirAsync(Guid colaboradorId, CriarDenunciaDto dto, CancellationToken cancellationToken);

    Task<IReadOnlyList<DenunciaResponse>> ListarAsync(
        Guid solicitanteId,
        bool podeVerTodas,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken);

    Task<DenunciaResponse?> ObterPorProtocoloAsync(
        string protocolo,
        Guid solicitanteId,
        bool podeVerTodas,
        CancellationToken cancellationToken);

    Task<DenunciaResponse?> RegistrarTratativaAsync(
        Guid denunciaId,
        string autor,
        RegistrarTratativaDto dto,
        CancellationToken cancellationToken);

    Task<ValidacaoDenunciaResponse?> ValidarAsync(
        Guid denunciaId,
        Guid colaboradorId,
        ValidarDenunciaDto dto,
        CancellationToken cancellationToken);

    Task<HistoricoTramitacaoResponse?> RegistrarTramitacaoAsync(
        string protocolo,
        RegistrarTramitacaoRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retorna null se a evidência não existir ou se o solicitante não puder ver a denúncia.
    /// </summary>
    Task<ArquivoEvidenciaDownload?> ObterEvidenciaAsync(
        string protocolo,
        Guid evidenciaId,
        Guid solicitanteId,
        bool podeVerTodas,
        CancellationToken cancellationToken);
}
