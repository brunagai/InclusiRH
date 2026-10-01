using InclusiRH.Api.Data;
using InclusiRH.Api.DTOs.Colaboradores;
using InclusiRH.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InclusiRH.Api.Services.Colaboradores;

public sealed class ColaboradorService : IColaboradorService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<Colaborador> _passwordHasher;

    public ColaboradorService(ApplicationDbContext db, IPasswordHasher<Colaborador> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<ColaboradorResponse>> ListarAsync(
        int pagina,
        int tamanho,
        CancellationToken cancellationToken)
    {
        pagina = Math.Max(1, pagina);
        tamanho = Math.Clamp(tamanho, 1, 100);

        var colaboradores = await _db.Colaboradores
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);

        return colaboradores.Select(Mapear).ToList();
    }

    public async Task<ColaboradorResponse?> ObterAsync(Guid id, CancellationToken cancellationToken)
    {
        var colaborador = await _db.Colaboradores
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return colaborador is null ? null : Mapear(colaborador);
    }

    public async Task<ColaboradorResponse> CadastrarAsync(
        CadastrarColaboradorRequest request,
        CancellationToken cancellationToken)
    {
        Enumeracao.GarantirDefinida(request.Papel, "papel");

        var email = request.Email.Trim().ToLowerInvariant();
        var emailEmUso = await _db.Colaboradores
            .AnyAsync(c => c.Email == email, cancellationToken);

        if (emailEmUso)
        {
            throw new RegraDeNegocioException("Já existe um colaborador cadastrado com este e-mail.");
        }

        var colaborador = new Colaborador
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = email,
            Cargo = request.Cargo.Trim(),
            Departamento = request.Departamento.Trim(),
            Papel = request.Papel,
            Ativo = true,
            DataCadastro = DateTimeOffset.UtcNow
        };

        colaborador.SenhaHash = _passwordHasher.HashPassword(colaborador, request.Senha);

        _db.Colaboradores.Add(colaborador);
        await _db.SaveChangesAsync(cancellationToken);

        return Mapear(colaborador);
    }

    private static ColaboradorResponse Mapear(Colaborador colaborador) => new()
    {
        Id = colaborador.Id,
        Nome = colaborador.Nome,
        Email = colaborador.Email,
        Cargo = colaborador.Cargo,
        Departamento = colaborador.Departamento,
        Papel = colaborador.Papel,
        Ativo = colaborador.Ativo,
        DataCadastro = colaborador.DataCadastro
    };
}
