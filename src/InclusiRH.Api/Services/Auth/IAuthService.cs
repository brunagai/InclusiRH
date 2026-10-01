using InclusiRH.Api.DTOs.Auth;
using InclusiRH.Api.DTOs.Colaboradores;

namespace InclusiRH.Api.Services.Auth;

public interface IAuthService
{
    Task<ColaboradorResponse> RegistrarAsync(
        RegistrarUsuarioRequest request,
        bool solicitanteEhAdministrador,
        CancellationToken cancellationToken);

    Task<LoginResponse?> EntrarAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Troca um refresh token válido por um novo par de tokens.
    /// Retorna null quando a sessão não pode ser renovada.
    /// </summary>
    Task<LoginResponse?> RenovarSessaoAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}
