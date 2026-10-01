using InclusiRH.Api.DTOs.Autodeclaracoes;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Autodeclaracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/colaboradores/{colaboradorId:guid}/autodeclaracao")]
public sealed class AutodeclaracoesController : ControllerBase
{
    private readonly IAutodeclaracaoService _autodeclaracoes;

    public AutodeclaracoesController(IAutodeclaracaoService autodeclaracoes)
    {
        _autodeclaracoes = autodeclaracoes;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AutodeclaracaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutodeclaracaoResponse>> Obter(
        Guid colaboradorId,
        CancellationToken cancellationToken)
    {
        if (UsuarioAtual.ObterId(User) != colaboradorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "A autodeclaração individual só pode ser consultada pela própria pessoa." });
        }

        var autodeclaracao = await _autodeclaracoes.ObterAsync(colaboradorId, cancellationToken);
        return autodeclaracao is null ? NotFound() : Ok(autodeclaracao);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AutodeclaracaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AutodeclaracaoResponse>> Salvar(
        Guid colaboradorId,
        SalvarAutodeclaracaoRequest request,
        CancellationToken cancellationToken)
    {
        if (UsuarioAtual.ObterId(User) != colaboradorId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "A autodeclaração individual só pode ser alterada pela própria pessoa." });
        }

        var salva = await _autodeclaracoes.SalvarAsync(colaboradorId, request, cancellationToken);
        return Ok(salva);
    }
}
