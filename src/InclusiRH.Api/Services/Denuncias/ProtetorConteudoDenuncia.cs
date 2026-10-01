using System.Security.Cryptography;
using System.Text;
using InclusiRH.Api.Services.Autodeclaracoes;
using InclusiRH.Api.Services.Seguranca;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Denuncias;

/// <summary>
/// Campos de texto livre da denúncia que são gravados cifrados.
/// </summary>
public enum CampoDenunciaProtegido
{
    Descricao,
    JustificativaContestacao
}

public interface IProtetorConteudoDenuncia
{
    byte[] Proteger(Guid denunciaId, CampoDenunciaProtegido campo, string texto);

    /// <summary>
    /// Lança <see cref="CryptographicException"/> se o pacote foi alterado
    /// ou não pertence a esta denúncia e campo.
    /// </summary>
    string Revelar(Guid denunciaId, CampoDenunciaProtegido campo, byte[] pacote);
}

/// <summary>
/// Cifra o relato e a justificativa de contestação com AES-256-GCM.
///
/// A chave é derivada da chave LGPD por HKDF-SHA256 com um rótulo próprio.
/// Assim, autodeclaração e denúncias não usam a mesma chave, e continua
/// existindo um único segredo para configurar.
///
/// O Id da denúncia e o nome do campo entram como dados associados. Copiar o
/// relato cifrado de um caso para outro, ou trocar descrição por justificativa,
/// faz a decifragem falhar.
/// </summary>
public sealed class ProtetorConteudoDenuncia : IProtetorConteudoDenuncia
{
    private static readonly byte[] RotuloDerivacao = Encoding.UTF8.GetBytes("InclusiRH:denuncias:v1");

    private readonly CifradorAesGcm _cifrador;

    public ProtetorConteudoDenuncia(IOptions<LgpdOptions> opcoes)
    {
        var chaveMestra = Encoding.UTF8.GetBytes(opcoes.Value.ChaveAes);
        var chaveDenuncias = HKDF.DeriveKey(HashAlgorithmName.SHA256, chaveMestra, 32, info: RotuloDerivacao);
        _cifrador = new CifradorAesGcm(chaveDenuncias);
    }

    public byte[] Proteger(Guid denunciaId, CampoDenunciaProtegido campo, string texto) =>
        _cifrador.Cifrar(Encoding.UTF8.GetBytes(texto), DadosAssociados(denunciaId, campo));

    public string Revelar(Guid denunciaId, CampoDenunciaProtegido campo, byte[] pacote) =>
        Encoding.UTF8.GetString(_cifrador.Decifrar(pacote, DadosAssociados(denunciaId, campo)));

    private static byte[] DadosAssociados(Guid denunciaId, CampoDenunciaProtegido campo) =>
        Encoding.UTF8.GetBytes($"{denunciaId:N}:{campo}");
}
