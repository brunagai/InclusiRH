using InclusiRH.Api.DTOs.Colaboradores;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Colaboradores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/colaboradores")]
public sealed class ColaboradoresController : ControllerBase
{
    private readonly IColaboradorService _colaboradores;

    public ColaboradoresController(IColaboradorService colaboradores)
    {
        _colaboradores = colaboradores;
    }

    [Authorize(Policy = PoliticasAcesso.Rh)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ColaboradorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ColaboradorResponse>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _colaboradores.ListarAsync(pagina, tamanho, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ColaboradorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ColaboradorResponse>> Obter(Guid id, CancellationToken cancellationToken)
    {
        if (!UsuarioAtual.PodeAcessarDadosDoColaborador(User, id))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { erro = "Você não tem permissão para consultar este colaborador." });
        }

        var colaborador = await _colaboradores.ObterAsync(id, cancellationToken);
        return colaborador is null ? NotFound() : Ok(colaborador);
    }

    [Authorize(Policy = PoliticasAcesso.Administrador)]
    [HttpPost]
    [ProducesResponseType(typeof(ColaboradorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ColaboradorResponse>> Cadastrar(
        CadastrarColaboradorRequest request,
        CancellationToken cancellationToken)
    {
        var criado = await _colaboradores.CadastrarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Obter), new { id = criado.Id }, criado);
    }
}
