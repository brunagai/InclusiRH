using InclusiRH.Api.DTOs.Auth;
using InclusiRH.Api.DTOs.Colaboradores;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Cadastro. Sem token, o papel permitido é Colaborador.
    /// Com token de administrador, qualquer papel pode ser atribuído.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ColaboradorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ColaboradorResponse>> Register(
        RegistrarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var solicitanteEhAdministrador = User.IsInRole(nameof(PapelColaborador.Administrador));
        var criado = await _auth.RegistrarAsync(request, solicitanteEhAdministrador, cancellationToken);
        return Created($"/api/colaboradores/{criado.Id}", criado);
    }

    /// <summary>
    /// Login. Devolve o access token (curto) e o refresh token (7 dias).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var sessao = await _auth.EntrarAsync(request, cancellationToken);
        if (sessao is null)
        {
            return Unauthorized(new { erro = "E-mail ou senha inválidos." });
        }

        return Ok(sessao);
    }

    /// <summary>
    /// Renova a sessão. Recebe o access token (pode estar vencido) e o refresh
    /// token ativo, e devolve um par novo. O refresh token enviado deixa de valer.
    /// </summary>
    /// <remarks>
    /// Fica anônimo porque o access token já venceu e o middleware JWT recusaria
    /// a chamada. A identidade é provada pela assinatura do access token somada
    /// ao refresh token.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var sessao = await _auth.RenovarSessaoAsync(request, cancellationToken);
        if (sessao is null)
        {
            // Mesma mensagem para token forjado, vencido, já usado ou conta inativa.
            return Unauthorized(new { erro = "Sessão expirada. Faça login novamente." });
        }

        return Ok(sessao);
    }
}
