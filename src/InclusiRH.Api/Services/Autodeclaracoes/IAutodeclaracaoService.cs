using InclusiRH.Api.DTOs.Autodeclaracoes;

namespace InclusiRH.Api.Services.Autodeclaracoes;

public interface IAutodeclaracaoService
{
    Task<AutodeclaracaoDto?> ObterPropriaAsync(Guid colaboradorId, CancellationToken cancellationToken);

    Task<AutodeclaracaoDto> SalvarPropriaAsync(
        Guid colaboradorId,
        AutodeclaracaoDto dto,
        CancellationToken cancellationToken);

    Task<RelatorioEsgDto> GerarRelatorioEsgAsync(CancellationToken cancellationToken);

    Task<AutodeclaracaoResponse?> ObterAsync(Guid colaboradorId, CancellationToken cancellationToken);

    Task<AutodeclaracaoResponse> SalvarAsync(
        Guid colaboradorId,
        SalvarAutodeclaracaoRequest request,
        CancellationToken cancellationToken);
}
