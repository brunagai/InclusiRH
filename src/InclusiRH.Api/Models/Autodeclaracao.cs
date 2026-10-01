namespace InclusiRH.Api.Models;

/// <summary>
/// Autodeclaração de diversidade. Os atributos sensíveis (LGPD, art. 5º, II)
/// não ficam em colunas legíveis: vão cifrados em <see cref="DadosProtegidos"/>.
/// </summary>
public sealed class Autodeclaracao
{
    public Guid Id { get; set; }

    public Guid ColaboradorId { get; set; }

    public byte[] DadosProtegidos { get; set; } = [];

    public bool ConsentimentoLgpd { get; set; }

    public DateTimeOffset DataConsentimento { get; set; }

    public DateTimeOffset DataAtualizacao { get; set; }

    public Colaborador Colaborador { get; set; } = null!;
}
