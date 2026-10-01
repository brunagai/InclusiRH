namespace InclusiRH.Api.Services;

internal static class Enumeracao
{
    public static void GarantirDefinida<TEnum>(TEnum valor, string campo)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(valor))
        {
            throw new RegraDeNegocioException($"O valor informado para {campo} não é válido.");
        }
    }
}
