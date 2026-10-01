using System.Text;

namespace InclusiRH.Api.Services.Evidencias;

/// <summary>
/// Arquivo que passou pela validação, já lido em memória.
/// </summary>
public sealed record ArquivoEvidenciaValidado(
    string NomeOriginal,
    string Extensao,
    string TipoArquivo,
    byte[] Conteudo);

/// <summary>
/// Aceita só PDF, PNG, JPG e áudios comuns (MP3, M4A, WAV, OGG).
///
/// A extensão sozinha não basta: um executável renomeado para .pdf passaria.
/// Por isso os primeiros bytes (assinatura) precisam bater com o formato que a
/// extensão promete. O Content-Type enviado pelo cliente é ignorado; o tipo
/// gravado vem desta tabela.
/// </summary>
public static class ValidadorEvidencias
{
    private sealed record TipoPermitido(string TipoArquivo, Func<byte[], bool> AssinaturaConfere);

    private static readonly IReadOnlyDictionary<string, TipoPermitido> TiposPorExtensao =
        new Dictionary<string, TipoPermitido>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new("application/pdf", b => ComecaCom(b, "%PDF-"u8)),
            [".png"] = new("image/png", b => ComecaCom(b, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])),
            [".jpg"] = new("image/jpeg", EhJpeg),
            [".jpeg"] = new("image/jpeg", EhJpeg),
            [".mp3"] = new("audio/mpeg", b => ComecaCom(b, "ID3"u8) || (b.Length > 1 && b[0] == 0xFF && (b[1] & 0xE0) == 0xE0)),
            [".m4a"] = new("audio/mp4", b => Contem(b, 4, "ftyp"u8)),
            [".wav"] = new("audio/wav", b => ComecaCom(b, "RIFF"u8) && Contem(b, 8, "WAVE"u8)),
            [".ogg"] = new("audio/ogg", b => ComecaCom(b, "OggS"u8))
        };

    public static string ExtensoesAceitas => string.Join(", ", TiposPorExtensao.Keys);

    /// <summary>
    /// Valida todos os arquivos antes de qualquer gravação. Se um falhar,
    /// nenhum é salvo e a requisição recebe 422 com o motivo.
    /// </summary>
    public static async Task<IReadOnlyList<ArquivoEvidenciaValidado>> ValidarAsync(
        IReadOnlyCollection<IFormFile>? arquivos,
        CancellationToken cancellationToken)
    {
        if (arquivos is null || arquivos.Count == 0)
        {
            return [];
        }

        if (arquivos.Count > LimitesEvidencia.QuantidadeMaximaPorEnvio)
        {
            throw new RegraDeNegocioException(
                $"Envie no máximo {LimitesEvidencia.QuantidadeMaximaPorEnvio} arquivos por vez.");
        }

        var validados = new List<ArquivoEvidenciaValidado>(arquivos.Count);
        foreach (var arquivo in arquivos)
        {
            var nome = LimparNome(arquivo.FileName);
            var extensao = Path.GetExtension(nome);

            if (!TiposPorExtensao.TryGetValue(extensao, out var tipo))
            {
                throw new RegraDeNegocioException(
                    $"O arquivo \"{nome}\" não é aceito. Formatos permitidos: {ExtensoesAceitas}.");
            }

            if (arquivo.Length == 0)
            {
                throw new RegraDeNegocioException($"O arquivo \"{nome}\" está vazio.");
            }

            if (arquivo.Length > LimitesEvidencia.TamanhoMaximoArquivoBytes)
            {
                throw new RegraDeNegocioException(
                    $"O arquivo \"{nome}\" passa de {LimitesEvidencia.TamanhoMaximoArquivoBytes / (1024 * 1024)} MB.");
            }

            var conteudo = await LerAsync(arquivo, cancellationToken);
            if (!tipo.AssinaturaConfere(conteudo))
            {
                throw new RegraDeNegocioException(
                    $"O conteúdo de \"{nome}\" não corresponde à extensão {extensao}.");
            }

            validados.Add(new ArquivoEvidenciaValidado(nome, extensao.ToLowerInvariant(), tipo.TipoArquivo, conteudo));
        }

        return validados;
    }

    private static async Task<byte[]> LerAsync(IFormFile arquivo, CancellationToken cancellationToken)
    {
        await using var origem = arquivo.OpenReadStream();
        using var destino = new MemoryStream((int)arquivo.Length);
        await origem.CopyToAsync(destino, cancellationToken);
        return destino.ToArray();
    }

    /// <summary>
    /// Descarta pastas ("../"), caracteres de controle e caracteres inválidos
    /// do nome. O nome limpo só é exibido; o arquivo em disco usa um GUID.
    /// </summary>
    private static string LimparNome(string? nomeEnviado)
    {
        var nome = Path.GetFileName((nomeEnviado ?? string.Empty).Replace('\\', '/'));
        var invalidos = Path.GetInvalidFileNameChars();
        var limpo = new StringBuilder(nome.Length);
        foreach (var caractere in nome)
        {
            if (!char.IsControl(caractere) && !invalidos.Contains(caractere))
            {
                limpo.Append(caractere);
            }
        }

        var resultado = limpo.ToString().Trim();
        if (resultado.Length == 0)
        {
            resultado = "arquivo";
        }

        if (resultado.Length > 200)
        {
            var extensao = Path.GetExtension(resultado);
            resultado = resultado[..(200 - extensao.Length)] + extensao;
        }

        return resultado;
    }

    private static bool EhJpeg(byte[] b) => ComecaCom(b, [0xFF, 0xD8, 0xFF]);

    private static bool ComecaCom(byte[] conteudo, ReadOnlySpan<byte> assinatura) =>
        Contem(conteudo, 0, assinatura);

    private static bool Contem(byte[] conteudo, int deslocamento, ReadOnlySpan<byte> assinatura) =>
        conteudo.Length >= deslocamento + assinatura.Length
        && conteudo.AsSpan(deslocamento, assinatura.Length).SequenceEqual(assinatura);
}
