using InclusiRH.Api.DTOs.Treinamentos;

namespace InclusiRH.Api.Services.Treinamentos;

public interface ITreinamentoService
{
    Task<IReadOnlyList<TreinamentoResponse>> ListarDisponiveisAsync(CancellationToken cancellationToken);

    Task<TreinamentoResponse> CadastrarAsync(CriarTreinamentoDto dto, CancellationToken cancellationToken);

    Task<ProgressoTreinamentoResponse> AtualizarProgressoAsync(
        Guid treinamentoId,
        Guid colaboradorId,
        AtualizarProgressoDto dto,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProgressoTreinamentoResponse>> ListarPorColaboradorAsync(
        Guid colaboradorId,
        CancellationToken cancellationToken);

    Task<RelatorioConclusaoDto> GerarRelatorioConclusaoAsync(CancellationToken cancellationToken);
}
