using InclusiRH.Api.DTOs.Colaboradores;

namespace InclusiRH.Api.Services.Colaboradores;

public interface IColaboradorService
{
    Task<IReadOnlyList<ColaboradorResponse>> ListarAsync(int pagina, int tamanho, CancellationToken cancellationToken);

    Task<ColaboradorResponse?> ObterAsync(Guid id, CancellationToken cancellationToken);

    Task<ColaboradorResponse> CadastrarAsync(CadastrarColaboradorRequest request, CancellationToken cancellationToken);
}
