using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Universidades.DTOs;

public class CrearUniversidadRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string AdminNombre { get; set; } = string.Empty;
    public string AdminCorreo { get; set; } = string.Empty;
}

public class UniversidadResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activa { get; set; }
}

public class UniversidadCreadaResponse
{
    public Guid Id { get; set; }
    public Guid AdminUsuarioId { get; set; }
    public string PasswordTemporal { get; set; } = string.Empty;
}

public class CambiarEstadoUniversidadRequest
{
    public bool Activa { get; set; }
}

namespace RetoAbierto.Application.Features.Universidades.Validators;

using RetoAbierto.Application.Features.Universidades.DTOs;

public class CrearUniversidadValidator : AbstractValidator<CrearUniversidadRequest>
{
    public CrearUniversidadValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la universidad es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre no debe superar los 150 caracteres.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo de la universidad es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        RuleFor(x => x.AdminNombre)
            .NotEmpty().WithMessage("El nombre del administrador es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del administrador no debe superar los 100 caracteres.");

        RuleFor(x => x.AdminCorreo)
            .NotEmpty().WithMessage("El correo del administrador es obligatorio.")
            .EmailAddress().WithMessage("El correo del administrador no tiene un formato válido.");
    }
}

namespace RetoAbierto.Application.Features.Universidades.Services;

using RetoAbierto.Application.Features.Universidades.DTOs;

public interface IUniversidadService
{
    Task<UniversidadCreadaResponse> CrearAsync(CrearUniversidadRequest request, CancellationToken ct = default);
    Task<List<UniversidadResponse>> ListarAsync(CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid id, bool activa, CancellationToken ct = default);
}

public class UniversidadService : IUniversidadService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public UniversidadService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<UniversidadCreadaResponse> CrearAsync(CrearUniversidadRequest request, CancellationToken ct = default)
    {
        if (_context.Universidades.Any(u => u.Correo.ToLower() == request.Correo.ToLower()))
            throw new ConflictException("Ya existe una universidad con ese correo institucional.");

        if (_context.Usuarios.Any(u => u.Correo.ToLower() == request.AdminCorreo.ToLower()))
            throw new ConflictException("Ya existe un usuario con ese correo de administrador.");

        var uni = new Universidad
        {
            Nombre = request.Nombre,
            Correo = request.Correo,
            Activa = true
        };
        await _context.AddAsync(uni, ct);

        string passwordTemporal = "UniPass" + Guid.NewGuid().ToString("N")[..6] + "!";
        var admin = new Usuario
        {
            Nombre = request.AdminNombre,
            Correo = request.AdminCorreo,
            PasswordHash = _passwordHasher.Hash(passwordTemporal),
            Activo = true,
            DebeCambiarPassword = true,
            FechaRegistro = DateTime.UtcNow
        };
        await _context.AddAsync(admin, ct);

        var uniUsuario = new UniversidadUsuario
        {
            UniversidadId = uni.Id,
            UsuarioId = admin.Id,
            Activo = true,
            FechaIngreso = DateTime.UtcNow
        };
        await _context.AddAsync(uniUsuario, ct);

        await _context.SaveChangesAsync(ct);

        return new UniversidadCreadaResponse
        {
            Id = uni.Id,
            AdminUsuarioId = admin.Id,
            PasswordTemporal = passwordTemporal
        };
    }

    public Task<List<UniversidadResponse>> ListarAsync(CancellationToken ct = default)
    {
        var lista = _context.Universidades
            .Select(u => new UniversidadResponse
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Correo = u.Correo,
                Activa = u.Activa
            }).ToList();

        return Task.FromResult(lista);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activa, CancellationToken ct = default)
    {
        var uni = _context.Universidades.FirstOrDefault(u => u.Id == id);
        if (uni == null)
            throw new NotFoundException("Universidad no encontrada.");

        uni.Activa = activa;
        await _context.SaveChangesAsync(ct);
    }
}

