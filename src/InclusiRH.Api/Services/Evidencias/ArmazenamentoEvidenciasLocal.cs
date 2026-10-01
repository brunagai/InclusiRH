using System.Security.Cryptography;
using System.Text;
using InclusiRH.Api.Services.Autodeclaracoes;
using InclusiRH.Api.Services.Seguranca;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Evidencias;

public interface IArmazenamentoEvidencias
{
    /// <summary>
    /// Cifra e grava o conteúdo. Retorna o caminho relativo a guardar no banco.
    /// </summary>
    Task<string> SalvarAsync(Guid evidenciaId, byte[] conteudo, CancellationToken cancellationToken);

    /// <summary>
    /// Lê e decifra. Lança <see cref="CryptographicException"/> se o arquivo
    /// foi alterado ou pertence a outra evidência.
    /// </summary>
    Task<byte[]> LerAsync(Guid evidenciaId, string caminhoArquivo, CancellationToken cancellationToken);

    void Remover(string caminhoArquivo);
}

/// <summary>
/// Guarda evidências em disco local, cifradas com AES-256-GCM.
///
/// Nome em disco: {Id da evidência}.bin. Sem extensão e sem o nome original,
/// ninguém com acesso à pasta sabe o tipo do arquivo nem consegue executá-lo.
/// A chave vem da chave LGPD via HKDF com rótulo próprio, e o Id da evidência
/// entra na verificação da cifra, então trocar dois arquivos de lugar é detectado.
/// </summary>
public sealed class ArmazenamentoEvidenciasLocal : IArmazenamentoEvidencias
{
    private static readonly byte[] RotuloDerivacao = Encoding.UTF8.GetBytes("InclusiRH:evidencias:v1");

    private readonly string _raiz;
    private readonly CifradorAesGcm _cifrador;

    public ArmazenamentoEvidenciasLocal(
        IOptions<EvidenciaOptions> opcoes,
        IOptions<LgpdOptions> lgpd,
        IWebHostEnvironment ambiente)
    {
        _raiz = Path.GetFullPath(Path.Combine(ambiente.ContentRootPath, opcoes.Value.Diretorio));
        Directory.CreateDirectory(_raiz);

        var chaveMestra = Encoding.UTF8.GetBytes(lgpd.Value.ChaveAes);
        _cifrador = new CifradorAesGcm(
            HKDF.DeriveKey(HashAlgorithmName.SHA256, chaveMestra, 32, info: RotuloDerivacao));
    }

    public async Task<string> SalvarAsync(Guid evidenciaId, byte[] conteudo, CancellationToken cancellationToken)
    {
        var caminho = $"{evidenciaId:N}.bin";
        var pacote = _cifrador.Cifrar(conteudo, evidenciaId.ToByteArray());

        // CreateNew nunca sobrescreve: se o GUID já existir, a gravação falha.
        await using var arquivo = new FileStream(
            ResolverCaminho(caminho), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await arquivo.WriteAsync(pacote, cancellationToken);
        return caminho;
    }

    public async Task<byte[]> LerAsync(Guid evidenciaId, string caminhoArquivo, CancellationToken cancellationToken)
    {
        var pacote = await File.ReadAllBytesAsync(ResolverCaminho(caminhoArquivo), cancellationToken);
        return _cifrador.Decifrar(pacote, evidenciaId.ToByteArray());
    }

    public void Remover(string caminhoArquivo)
    {
        var completo = ResolverCaminho(caminhoArquivo);
        if (File.Exists(completo))
        {
            File.Delete(completo);
        }
    }

    /// <summary>
    /// Mesmo vindo do banco, o caminho é conferido: ele precisa ficar dentro
    /// da pasta de evidências. Um valor como "..\appsettings.json" é recusado.
    /// </summary>
    private string ResolverCaminho(string caminhoRelativo)
    {
        var completo = Path.GetFullPath(Path.Combine(_raiz, caminhoRelativo));
        if (!completo.StartsWith(_raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de evidência fora da pasta permitida.");
        }

        return completo;
    }
}
