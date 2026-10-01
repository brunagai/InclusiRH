using InclusiRH.Api.DTOs.Denuncias;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Auth;
using InclusiRH.Api.Services.Denuncias;
using InclusiRH.Api.Services.Evidencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InclusiRH.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/denuncias")]
public sealed class DenunciasController : ControllerBase
{
    private readonly IDenunciaService _denuncias;

    public DenunciasController(IDenunciaService denuncias)
    {
        _denuncias = denuncias;
    }

    /// <summary>
    /// Abre uma denúncia em nome de quem está autenticado, com evidências opcionais.
    /// IsAnonima esconde a identidade nas consultas do RH, mas preserva o vínculo para a validação.
    /// </summary>
    /// <remarks>
    /// multipart/form-data: tipoOcorrencia, descricao, isAnonima e, opcionalmente,
    /// um ou mais campos "evidencias" (até 5 arquivos de 10 MB: PDF, PNG, JPG, MP3, M4A, WAV, OGG).
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitesEvidencia.TamanhoMaximoRequisicaoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesEvidencia.TamanhoMaximoRequisicaoBytes)]
    [ProducesResponseType(typeof(DenunciaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DenunciaResponse>> Abrir(
        [FromForm] CriarDenunciaDto dto,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var criada = await _denuncias.AbrirAsync(colaboradorId.Value, dto, cancellationToken);
        return CreatedAtAction(nameof(ObterPorProtocolo), new { protocolo = criada.Protocolo }, criada);
    }

    /// <summary>
    /// Colaborador vê só as próprias denúncias. RH e Admin veem todas,
    /// sem a identidade das que foram abertas como anônimas.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DenunciaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DenunciaResponse>>> Listar(
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken cancellationToken = default)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var denuncias = await _denuncias.ListarAsync(
            colaboradorId.Value,
            PodeVerTodas(),
            pagina,
            tamanho,
            cancellationToken);

        return Ok(denuncias);
    }

    [HttpGet("{protocolo}")]
    [ProducesResponseType(typeof(DenunciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DenunciaResponse>> ObterPorProtocolo(
        string protocolo,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var denuncia = await _denuncias.ObterPorProtocoloAsync(
            protocolo,
            colaboradorId.Value,
            PodeVerTodas(),
            cancellationToken);

        return denuncia is null ? NotFound() : Ok(denuncia);
    }

    [Authorize(Policy = PoliticasAcesso.Rh)]
    [HttpPut("{id:guid}/tratativa")]
    [ProducesResponseType(typeof(DenunciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DenunciaResponse>> RegistrarTratativa(
        Guid id,
        RegistrarTratativaDto dto,
        CancellationToken cancellationToken)
    {
        var tratada = await _denuncias.RegistrarTratativaAsync(id, AutorAtual(), dto, cancellationToken);
        return tratada is null ? NotFound() : Ok(tratada);
    }

    /// <summary>
    /// A pessoa que abriu o caso confirma (Encerrado) ou contesta (Contestado).
    /// A contestação dispara o alerta de escalonamento e pode trazer novas evidências.
    /// </summary>
    /// <remarks>
    /// multipart/form-data: statusValidacao, justificativaContestacao e,
    /// só ao contestar, campos "evidencias" com os mesmos limites da abertura.
    /// </remarks>
    [HttpPut("{id:guid}/validacao")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimitesEvidencia.TamanhoMaximoRequisicaoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesEvidencia.TamanhoMaximoRequisicaoBytes)]
    [ProducesResponseType(typeof(ValidacaoDenunciaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ValidacaoDenunciaResponse>> Validar(
        Guid id,
        [FromForm] ValidarDenunciaDto dto,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var validacao = await _denuncias.ValidarAsync(id, colaboradorId.Value, dto, cancellationToken);
        return validacao is null ? NotFound() : Ok(validacao);
    }

    /// <summary>
    /// Baixa uma evidência decifrada. Mesma regra de acesso da denúncia:
    /// a autora do caso ou RH/Admin. Para os demais, 404.
    /// </summary>
    [HttpGet("{protocolo}/evidencias/{evidenciaId:guid}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> BaixarEvidencia(
        string protocolo,
        Guid evidenciaId,
        CancellationToken cancellationToken)
    {
        var colaboradorId = UsuarioAtual.ObterId(User);
        if (colaboradorId is null)
        {
            return Unauthorized(new { erro = "Autenticação necessária." });
        }

        var arquivo = await _denuncias.ObterEvidenciaAsync(
            protocolo,
            evidenciaId,
            colaboradorId.Value,
            PodeVerTodas(),
            cancellationToken);

        if (arquivo is null)
        {
            return NotFound();
        }

        // Sempre como anexo e sem adivinhação de tipo pelo navegador:
        // um arquivo enviado nunca é interpretado como página da API.
        // no-store evita que proxies e o navegador guardem a evidência.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "no-store";
        return File(arquivo.Conteudo, arquivo.TipoArquivo, arquivo.NomeArquivo);
    }

    [Authorize(Policy = PoliticasAcesso.ComiteOuRh)]
    [HttpPost("{protocolo}/tramitacoes")]
    [ProducesResponseType(typeof(HistoricoTramitacaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HistoricoTramitacaoResponse>> RegistrarTramitacao(
        string protocolo,
        RegistrarTramitacaoRequest request,
        CancellationToken cancellationToken)
    {
        request.Autor = AutorAtual();
        var historico = await _denuncias.RegistrarTramitacaoAsync(protocolo, request, cancellationToken);
        return historico is null
            ? NotFound()
            : CreatedAtAction(nameof(ObterPorProtocolo), new { protocolo }, historico);
    }

    private bool PodeVerTodas() =>
        UsuarioAtual.PossuiPapel(User, PapelColaborador.RecursosHumanos, PapelColaborador.Administrador);

    private string AutorAtual()
    {
        var nome = UsuarioAtual.ObterNome(User);
        var papel = User.FindFirst("role")?.Value;
        if (string.IsNullOrWhiteSpace(nome))
        {
            return papel ?? "RH";
        }

        return string.IsNullOrWhiteSpace(papel) ? nome : $"{nome} ({papel})";
    }
}
