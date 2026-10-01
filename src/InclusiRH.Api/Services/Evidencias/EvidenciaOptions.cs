namespace InclusiRH.Api.Services.Evidencias;

public sealed class EvidenciaOptions
{
    public const string SectionName = "Evidencias";

    /// <summary>
    /// Pasta dos arquivos cifrados, relativa à raiz do projeto (ContentRoot).
    /// Fica fora de wwwroot: nada ali é servido como arquivo estático.
    /// </summary>
    public string Diretorio { get; set; } = "Uploads/Evidencias";
}

/// <summary>
/// Limites fixos de upload. São constantes porque também alimentam os
/// atributos [RequestSizeLimit] das rotas.
/// </summary>
public static class LimitesEvidencia
{
    public const int QuantidadeMaximaPorEnvio = 5;

    public const long TamanhoMaximoArquivoBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Cinco arquivos no limite, mais 1 MB para os campos de texto do formulário.
    /// </summary>
    public const long TamanhoMaximoRequisicaoBytes =
        QuantidadeMaximaPorEnvio * TamanhoMaximoArquivoBytes + 1024 * 1024;
}
