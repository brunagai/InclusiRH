using System.Text.Json;
using System.Text.Json.Serialization;

namespace InclusiRH.Api.Models.Enums;

/// <summary>
/// Aceita os nomes do enum e os apelidos usados no negócio: RH e Admin.
/// </summary>
public sealed class PapelColaboradorJsonConverter : JsonConverter<PapelColaborador>
{
    public override PapelColaborador Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("O papel deve ser informado como texto.");
        }

        var texto = reader.GetString();
        if (string.IsNullOrWhiteSpace(texto))
        {
            throw new JsonException("Informe o papel.");
        }

        if (texto.Equals("RH", StringComparison.OrdinalIgnoreCase))
        {
            return PapelColaborador.RecursosHumanos;
        }

        if (texto.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return PapelColaborador.Administrador;
        }

        if (Enum.TryParse<PapelColaborador>(texto, ignoreCase: true, out var papel) && Enum.IsDefined(papel))
        {
            return papel;
        }

        throw new JsonException(
            "Papel inválido. Use Colaborador, Gestor, RecursosHumanos (RH), ComiteDei ou Administrador (Admin).");
    }

    public override void Write(Utf8JsonWriter writer, PapelColaborador value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
