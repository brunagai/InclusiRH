using System.Security.Cryptography;

namespace InclusiRH.Api.Services.Seguranca;

/// <summary>
/// Núcleo AES-256-GCM compartilhado pelos dados sensíveis da API.
///
/// Pacote gravado: [versão 1 byte][nonce 12 bytes][tag 16 bytes][texto cifrado].
/// Cada chamada usa um nonce aleatório novo, então o mesmo texto gera pacotes
/// diferentes e o banco não revela relatos repetidos.
///
/// Os dados associados (AAD) não são cifrados, mas entram na tag. Se o pacote
/// for copiado para outro registro ou outro campo, a verificação falha.
/// </summary>
public sealed class CifradorAesGcm
{
    private const byte Versao = 1;
    private const int TamanhoNonce = 12;
    private const int TamanhoTag = 16;
    private const int TamanhoCabecalho = 1 + TamanhoNonce + TamanhoTag;

    private readonly byte[] _chave;

    public CifradorAesGcm(byte[] chave)
    {
        if (chave.Length != 32)
        {
            throw new ArgumentException("AES-256 exige chave de 32 bytes.", nameof(chave));
        }

        _chave = chave;
    }

    public byte[] Cifrar(ReadOnlySpan<byte> claro, ReadOnlySpan<byte> dadosAssociados = default)
    {
        var pacote = new byte[TamanhoCabecalho + claro.Length];
        pacote[0] = Versao;

        var nonce = pacote.AsSpan(1, TamanhoNonce);
        var tag = pacote.AsSpan(1 + TamanhoNonce, TamanhoTag);
        var cifrado = pacote.AsSpan(TamanhoCabecalho);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Encrypt(nonce, claro, cifrado, tag, dadosAssociados);
        return pacote;
    }

    /// <summary>
    /// Lança <see cref="CryptographicException"/> se o pacote tiver sido
    /// alterado, pertencer a outro registro ou tiver sido gerado com outra chave.
    /// </summary>
    public byte[] Decifrar(byte[] pacote, ReadOnlySpan<byte> dadosAssociados = default)
    {
        if (pacote.Length < TamanhoCabecalho || pacote[0] != Versao)
        {
            throw new CryptographicException("Pacote cifrado inválido.");
        }

        var nonce = pacote.AsSpan(1, TamanhoNonce);
        var tag = pacote.AsSpan(1 + TamanhoNonce, TamanhoTag);
        var cifrado = pacote.AsSpan(TamanhoCabecalho);
        var claro = new byte[cifrado.Length];

        using var aes = new AesGcm(_chave, TamanhoTag);
        aes.Decrypt(nonce, cifrado, tag, claro, dadosAssociados);
        return claro;
    }
}
