using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Seguranca;
using Microsoft.Extensions.Options;

namespace InclusiRH.Api.Services.Autodeclaracoes;

public interface IProtetorDadosSensiveis
{
    byte[] Proteger(DadosAutodeclaracaoProtegidos dados);

    DadosAutodeclaracaoProtegidos Revelar(byte[] protegido);
}

public sealed class DadosAutodeclaracaoProtegidos
{
    public Etnia Etnia { get; set; }

    public Genero Genero { get; set; }

    public OrientacaoSexual OrientacaoSexual { get; set; }

    public SituacaoPcd SituacaoPcd { get; set; }

    public string? DescricaoDeficiencia { get; set; }

    public ContextoSocioeconomico ContextoSocioeconomico { get; set; }
}

/// <summary>
/// Cifra a autodeclaração com AES-256-GCM antes de gravar.
/// Sem a chave, o banco não revela etnia, gênero, orientação, PCD ou contexto.
/// </summary>
public sealed class ProtetorDadosSensiveisAes : IProtetorDadosSensiveis
{
    private readonly CifradorAesGcm _cifrador;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ProtetorDadosSensiveisAes(IOptions<LgpdOptions> opcoes)
    {
        _cifrador = new CifradorAesGcm(System.Text.Encoding.UTF8.GetBytes(opcoes.Value.ChaveAes));
    }

    public byte[] Proteger(DadosAutodeclaracaoProtegidos dados) =>
        _cifrador.Cifrar(JsonSerializer.SerializeToUtf8Bytes(dados, JsonOptions));

    public DadosAutodeclaracaoProtegidos Revelar(byte[] protegido) =>
        JsonSerializer.Deserialize<DadosAutodeclaracaoProtegidos>(_cifrador.Decifrar(protegido), JsonOptions)
            ?? throw new CryptographicException("Autodeclaração protegida sem conteúdo.");
}
