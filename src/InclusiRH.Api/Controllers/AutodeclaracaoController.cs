using InclusiRH.Api.DTOs.Autodeclaracoes;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Autodeclaracoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/autodeclaracao")]
public sealed class AutodeclaracaoController : ControllerBase
{
    private readonly IAutodeclaracaoService _autodeclaracoes;

    public AutodeclaracaoController(IAutodeclaracaoService autodeclaracoes)
    {
        _autodeclaracoes = autodeclaracoes;
    }

    /// <summary>
    /// Grava ou atualiza a autodeclaração de quem está autenticado.
    /// Os atributos sensíveis são cifrados com AES-256-GCM antes de irem ao banco.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AutodeclaracaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AutodeclaracaoDto>> Salvar(
        AutodeclaracaoDto dto,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var salva = await _autodeclaracoes.SalvarPropriaAsync(colaboradorId.Value, dto, cancellationToken);
        return Ok(salva);
    }

    [HttpGet]
    [ProducesResponseType(typeof(AutodeclaracaoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutodeclaracaoDto>> Obter(CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var autodeclaracao = await _autodeclaracoes.ObterPropriaAsync(colaboradorId.Value, cancellationToken);
        return autodeclaracao is null ? NotFound() : Ok(autodeclaracao);
    }

    /// <summary>
    /// Indicadores agregados para RH e Admin. Não devolve registro individual.
    /// Grupos com menos de 5 pessoas são suprimidos.
    /// </summary>
    [Authorize(Policy = PoliticasAcesso.Rh)]
    [HttpGet("relatorios-esg")]
    [ProducesResponseType(typeof(RelatorioEsgDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RelatorioEsgDto>> RelatorioEsg(CancellationToken cancellationToken)
    {
        var relatorio = await _autodeclaracoes.GerarRelatorioEsgAsync(cancellationToken);
        return Ok(relatorio);
    }
}
