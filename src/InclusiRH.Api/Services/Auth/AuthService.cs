using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InclusiRH.Api.Data;
using InclusiRH.Api.DTOs.Auth;
using InclusiRH.Api.DTOs.Colaboradores;
using InclusiRH.Api.Models;
using InclusiRH.Api.Models.Enums;
using InclusiRH.Api.Services.Colaboradores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InclusiRH.Api.Services.Auth;

/// <summary>
/// Autenticação com par de tokens.
///
/// Access token: JWT assinado (HS256), vida curta, enviado em toda requisição.
/// Refresh token: 64 bytes aleatórios, vida longa, usado só em /api/auth/refresh.
///
/// Renovação de sessão:
/// 1. O app envia o access token vencido e o refresh token.
/// 2. A assinatura do access token é conferida, ignorando só o vencimento,
///    para descobrir de quem é a sessão sem confiar em dado adulterado.
/// 3. O hash do refresh token recebido precisa ser igual ao gravado para esse
///    colaborador, e a validade precisa estar no futuro.
/// 4. Um novo par é emitido e o refresh token antigo é substituído na mesma
///    instrução SQL (rotação). Um refresh token só serve uma vez.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const int TamanhoRefreshTokenBytes = 64;

    private readonly ApplicationDbContext _db;
    private readonly IColaboradorService _colaboradores;
    private readonly IPasswordHasher<Colaborador> _passwordHasher;
    private readonly JwtSettings _jwt;

    public AuthService(
        ApplicationDbContext db,
        IColaboradorService colaboradores,
        IPasswordHasher<Colaborador> passwordHasher,
        IOptions<JwtSettings> jwt)
    {
        _db = db;
        _colaboradores = colaboradores;
        _passwordHasher = passwordHasher;
        _jwt = jwt.Value;
    }

    public async Task<ColaboradorResponse> RegistrarAsync(
        RegistrarUsuarioRequest request,
        bool solicitanteEhAdministrador,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(request.Papel, "papel");

        // Cadastro público só cria Colaborador. RH, Admin, comitê e gestor
        // exigem um administrador já autenticado. A primeira conta da base
        // pode escolher o papel, para existir alguém capaz de operar o sistema.
        var baseVazia = !await _db.Colaboradores.AnyAsync(cancellationToken);
        var papelElevado = request.Papel != PapelColaborador.Colaborador;
        if (papelElevado && !baseVazia && !solicitanteEhAdministrador)
        {
            throw new RegraDeNegocioException(
                "Apenas um administrador pode atribuir papéis diferentes de Colaborador.");
        }

        return await _colaboradores.CadastrarAsync(new CadastrarColaboradorRequest
        {
            Nome = request.Nome,
            Email = request.Email,
            Senha = request.Senha,
            Cargo = request.Cargo,
            Departamento = request.Departamento,
            Papel = request.Papel
        }, cancellationToken);
    }

    public async Task<LoginResponse?> EntrarAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var colaborador = await _db.Colaboradores
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);

        // A mesma resposta para e-mail inexistente, senha errada ou conta inativa
        // evita revelar quais contas existem.
        if (colaborador is null || !colaborador.Ativo)
        {
            return null;
        }

        var verificacao = _passwordHasher.VerifyHashedPassword(colaborador, colaborador.SenhaHash, request.Senha);
        if (verificacao == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificacao == PasswordVerificationResult.SuccessRehashNeeded)
        {
            colaborador.SenhaHash = _passwordHasher.HashPassword(colaborador, request.Senha);
        }

        // Cada colaborador guarda um único refresh token. Um novo login
        // substitui o anterior, encerrando a renovação da sessão antiga.
        var refreshToken = GerarRefreshToken();
        var refreshExpiraEm = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpireDays);
        colaborador.RefreshToken = CalcularHash(refreshToken);
        colaborador.RefreshTokenExpiryTime = refreshExpiraEm;
        await _db.SaveChangesAsync(cancellationToken);

        return MontarResposta(colaborador, refreshToken, refreshExpiraEm);
    }

    public async Task<LoginResponse?> RenovarSessaoAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var colaboradorId = ObterColaboradorDoTokenExpirado(request.AccessToken);
        if (colaboradorId is null)
        {
            return null;
        }

        var colaborador = await _db.Colaboradores
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == colaboradorId.Value, cancellationToken);

        // Conta desativada perde a renovação mesmo com refresh token no prazo.
        if (colaborador is null || !colaborador.Ativo)
        {
            return null;
        }

        var hashRecebido = CalcularHash(request.RefreshToken);
        var agora = DateTime.UtcNow;
        var novoRefreshToken = GerarRefreshToken();
        var novoRefreshExpiraEm = agora.AddDays(_jwt.RefreshTokenExpireDays);
        var novoHash = CalcularHash(novoRefreshToken);

        // A troca só acontece se o hash gravado ainda for o recebido e estiver
        // no prazo. Duas renovações simultâneas com o mesmo token disputam esta
        // linha: a primeira grava o novo hash e a segunda encontra zero linhas.
        var linhasAtualizadas = await _db.Colaboradores
            .Where(c => c.Id == colaborador.Id
                && c.Ativo
                && c.RefreshToken == hashRecebido
                && c.RefreshTokenExpiryTime > agora)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.RefreshToken, novoHash)
                .SetProperty(c => c.RefreshTokenExpiryTime, novoRefreshExpiraEm),
                cancellationToken);

        if (linhasAtualizadas == 0)
        {
            return null;
        }

        // O novo access token usa o papel atual do banco: uma mudança de papel
        // feita pelo administrador passa a valer na próxima renovação.
        return MontarResposta(colaborador, novoRefreshToken, novoRefreshExpiraEm);
    }

    private LoginResponse MontarResposta(Colaborador colaborador, string refreshToken, DateTime refreshExpiraEm)
    {
        var accessExpiraEm = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenExpireMinutes);
        return new LoginResponse
        {
            AccessToken = GerarAccessToken(colaborador, accessExpiraEm),
            AccessTokenExpiraEm = accessExpiraEm,
            RefreshToken = refreshToken,
            RefreshTokenExpiraEm = new DateTimeOffset(refreshExpiraEm, TimeSpan.Zero),
            ColaboradorId = colaborador.Id,
            Nome = colaborador.Nome,
            Papel = colaborador.Papel
        };
    }

    private string GerarAccessToken(Colaborador colaborador, DateTimeOffset expiraEm)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, colaborador.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, colaborador.Email),
            new(JwtRegisteredClaimNames.Name, colaborador.Nome),
            new("role", colaborador.Papel.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var credenciais = new SigningCredentials(ObterChaveAssinatura(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Lê o colaborador (claim sub) de um access token possivelmente vencido.
    /// Emissor, audiência, algoritmo e assinatura continuam obrigatórios;
    /// só o prazo é ignorado. Token forjado ou de outro sistema retorna null.
    /// </summary>
    private Guid? ObterColaboradorDoTokenExpirado(string accessToken)
    {
        var parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
            ValidIssuer = _jwt.Issuer,
            ValidAudience = _jwt.Audience,
            IssuerSigningKey = ObterChaveAssinatura(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = handler.ValidateToken(accessToken, parametros, out _);
            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private SymmetricSecurityKey ObterChaveAssinatura() =>
        new(Encoding.UTF8.GetBytes(_jwt.Key));

    /// <summary>
    /// 64 bytes de gerador criptográfico em Base64 URL-safe (86 caracteres).
    /// </summary>
    private static string GerarRefreshToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TamanhoRefreshTokenBytes));

    /// <summary>
    /// O banco guarda só o SHA-256 do refresh token. Como o valor original tem
    /// 512 bits aleatórios, dispensa salt ou hash lento como o de senha.
    /// </summary>
    private static string CalcularHash(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
}
