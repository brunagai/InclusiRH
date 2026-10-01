using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InclusiRH.Api.Models.Enums;

namespace InclusiRH.Api.Services.Auth;

public static class UsuarioAtual
{
    public static Guid? ObterId(ClaimsPrincipal usuario)
    {
        var valor = usuario.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(valor, out var id) ? id : null;
    }

    public static string? ObterNome(ClaimsPrincipal usuario) =>
        usuario.FindFirst(JwtRegisteredClaimNames.Name)?.Value;

    public static bool PossuiPapel(ClaimsPrincipal usuario, params PapelColaborador[] papeis) =>
        papeis.Any(papel => usuario.IsInRole(papel.ToString()));

    public static bool PodeAcessarDadosDoColaborador(ClaimsPrincipal usuario, Guid colaboradorId)
    {
        if (PossuiPapel(usuario, PapelColaborador.RecursosHumanos, PapelColaborador.Administrador))
        {
            return true;
        }

        return ObterId(usuario) == colaboradorId;
    }
}
