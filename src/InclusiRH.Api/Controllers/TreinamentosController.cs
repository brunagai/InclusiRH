using InclusiRH.Api.DTOs.Treinamentos;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Treinamentos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/treinamentos")]
public sealed class TreinamentosController : ControllerBase
{
    private readonly ITreinamentoService _treinamentos;

    public TreinamentosController(ITreinamentoService treinamentos)
    {
        _treinamentos = treinamentos;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TreinamentoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TreinamentoResponse>>> Listar(CancellationToken cancellationToken)
    {
        var treinamentos = await _treinamentos.ListarDisponiveisAsync(cancellationToken);
        return Ok(treinamentos);
    }

    [Authorize(Policy = PoliticasAcesso.Rh)]
    [HttpPost]
    [ProducesResponseType(typeof(TreinamentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TreinamentoResponse>> Cadastrar(
        CriarTreinamentoDto dto,
        CancellationToken cancellationToken)
    {
        var criado = await _treinamentos.CadastrarAsync(dto, cancellationToken);
        return Created($"/api/treinamentos/{criado.Id}", criado);
    }

    /// <summary>
    /// A pessoa autenticada inicia ou atualiza o próprio progresso.
    /// Conclusão não regride, para preservar a evidência de cumprimento das leis.
    /// </summary>
    [HttpPost("{id:guid}/progresso")]
    [ProducesResponseType(typeof(ProgressoTreinamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProgressoTreinamentoResponse>> AtualizarProgresso(
        Guid id,
        AtualizarProgressoDto dto,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var progresso = await _treinamentos.AtualizarProgressoAsync(id, colaboradorId.Value, dto, cancellationToken);
        return Ok(progresso);
    }

    /// <summary>
    /// Taxa de conclusão dos treinamentos obrigatórios por departamento.
    /// Métrica do pilar Social (S), sem identificar quem concluiu.
    /// </summary>
    [Authorize(Policy = PoliticasAcesso.Rh)]
    [HttpGet("relatorios-conclusao")]
    [ProducesResponseType(typeof(RelatorioConclusaoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RelatorioConclusaoDto>> RelatorioConclusao(CancellationToken cancellationToken)
    {
        var relatorio = await _treinamentos.GerarRelatorioConclusaoAsync(cancellationToken);
        return Ok(relatorio);
    }

    [HttpGet("/api/colaboradores/{colaboradorId:guid}/treinamentos")]
    [ProducesResponseType(typeof(IReadOnlyList<ProgressoTreinamentoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProgressoTreinamentoResponse>>> ListarPorColaborador(
        Guid colaboradorId,
        CancellationToken cancellationToken)
    {
        if (!UsuarioAtual.PodeAcessarDadosDoColaborador(User, colaboradorId))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você não tem permissão para consultar estes treinamentos." });
        }

        var progressos = await _treinamentos.ListarPorColaboradorAsync(colaboradorId, cancellationToken);
        return Ok(progressos);
    }
}
