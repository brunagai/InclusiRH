using InclusiRH.Api.Services.Denuncias;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Background;

/// <summary>
/// Varre periodicamente as denúncias e dispara os alertas formais:
/// contestações ainda não notificadas e casos acima do prazo de tratativa.
///
/// Isolamento da API:
/// - Roda fora das requisições HTTP, com escopo de DI próprio por varredura.
/// - Nenhuma exceção sai do laço. No .NET 8, uma exceção não tratada num
///   BackgroundService derruba a aplicação inteira.
/// - O cancelamento no desligamento da API interrompe a espera e a varredura
///   em andamento sem gerar erro.
/// </summary>
public sealed class AlertaEscalonamentoWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AlertaEscalonamentoOptions _opcoes;
    private readonly ILogger<AlertaEscalonamentoWorker> _logger;

    public AlertaEscalonamentoWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AlertaEscalonamentoOptions> opcoes,
        ILogger<AlertaEscalonamentoWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _opcoes = opcoes.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_opcoes.Habilitado)
        {
            _logger.LogInformation("Worker de alertas de escalonamento desabilitado.");
            return;
        }

        _logger.LogInformation(
            "Worker de alertas iniciado: varredura a cada {IntervaloMinutos} min, prazo de tratativa de {PrazoTratativaDias} dia(s).",
            _opcoes.IntervaloMinutos,
            _opcoes.PrazoTratativaDias);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_opcoes.AtrasoInicialSegundos), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_opcoes.IntervaloMinutos));
            do
            {
                await ExecutarVarreduraAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Desligamento normal da API.
        }

        _logger.LogInformation("Worker de alertas encerrado.");
    }

    private async Task ExecutarVarreduraAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var escopo = _scopeFactory.CreateAsyncScope();
            var processador = escopo.ServiceProvider.GetRequiredService<IProcessadorAlertasDenuncia>();

            var contestacoes = await processador.ProcessarContestacoesAsync(stoppingToken);
            var prazos = await processador.ProcessarPrazosExcedidosAsync(stoppingToken);

            if (contestacoes.Enviados + contestacoes.Falhas + prazos.Enviados + prazos.Falhas > 0)
            {
                _logger.LogInformation(
                    "Varredura de alertas: contestações {ContestacoesEnviadas} enviada(s) e {ContestacoesFalhas} falha(s); prazos {PrazosEnviados} enviado(s) e {PrazosFalhas} falha(s).",
                    contestacoes.Enviados,
                    contestacoes.Falhas,
                    prazos.Enviados,
                    prazos.Falhas);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Banco indisponível, por exemplo. A próxima varredura tenta de novo.
            _logger.LogError(ex, "Varredura de alertas falhou; nova tentativa no próximo ciclo.");
        }
    }
}
