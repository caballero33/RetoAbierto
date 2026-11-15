using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Common.Models;
using RetoAbierto.Domain.Entities;

namespace RetoAbierto.Application.Features.Estudiantes.DTOs;

public class RegistrarEstudianteRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public int DisponibilidadHoras { get; set; }
    public string? Experiencia { get; set; }
}

public class EstudianteCreadoResponse
{
    public Guid EstudianteId { get; set; }
    public Guid UsuarioId { get; set; }
    public string PasswordTemporal { get; set; } = string.Empty;
}

public class ActualizarPerfilRequest
{
    public string Carrera { get; set; } = string.Empty;
    public int DisponibilidadHoras { get; set; }
    public string? Experiencia { get; set; }
}

public class EstudianteResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class HabilidadEstudianteItemDto
{
    public Guid HabilidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Nivel { get; set; }
}

public class PerfilEstudianteResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Universidad { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public int DisponibilidadHoras { get; set; }
    public string? Experiencia { get; set; }
    public bool Activo { get; set; }
    public List<HabilidadEstudianteItemDto> Habilidades { get; set; } = new();
}

public class CambiarEstadoEstudianteRequest
{
    public bool Activo { get; set; }
}

namespace RetoAbierto.Application.Features.Estudiantes.Validators;

using RetoAbierto.Application.Features.Estudiantes.DTOs;

public class RegistrarEstudianteValidator : AbstractValidator<RegistrarEstudianteRequest>
{
    public RegistrarEstudianteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe superar los 100 caracteres.");

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo es inválido.");

        RuleFor(x => x.Carrera)
            .NotEmpty().WithMessage("La carrera es obligatoria.")
            .MaximumLength(150).WithMessage("La carrera no debe superar los 150 caracteres.");

        RuleFor(x => x.DisponibilidadHoras)
            .InclusiveBetween(0, 80).WithMessage("La disponibilidad de horas debe estar entre 0 y 80.");
    }
}

public class ActualizarPerfilValidator : AbstractValidator<ActualizarPerfilRequest>
{
    public ActualizarPerfilValidator()
    {
        RuleFor(x => x.Carrera)
            .NotEmpty().WithMessage("La carrera es obligatoria.")
            .MaximumLength(150).WithMessage("La carrera no debe superar los 150 caracteres.");

        RuleFor(x => x.DisponibilidadHoras)
            .InclusiveBetween(0, 80).WithMessage("La disponibilidad de horas debe estar entre 0 y 80.");
    }
}

namespace RetoAbierto.Application.Features.Estudiantes.Services;

using RetoAbierto.Application.Features.Estudiantes.DTOs;

public interface IEstudianteService
{
    Task<EstudianteCreadoResponse> RegistrarAsync(Guid universidadId, Guid registradoPorUsuarioId, RegistrarEstudianteRequest request, CancellationToken ct = default);
    Task<PagedResult<EstudianteResponse>> ListarPorUniversidadAsync(Guid universidadId, bool? activo, string? q, int pagina, int tamano, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid universidadId, Guid estudianteId, bool activo, CancellationToken ct = default);
    Task<PerfilEstudianteResponse> ObtenerMiPerfilAsync(Guid usuarioId, CancellationToken ct = default);
    Task<PerfilEstudianteResponse> ActualizarMiPerfilAsync(Guid usuarioId, ActualizarPerfilRequest request, CancellationToken ct = default);
}

public class EstudianteService : IEstudianteService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public EstudianteService(IApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<EstudianteCreadoResponse> RegistrarAsync(Guid universidadId, Guid registradoPorUsuarioId, RegistrarEstudianteRequest request, CancellationToken ct = default)
    {
        if (_context.Usuarios.Any(u => u.Correo.ToLower() == request.Correo.ToLower()))
            throw new ConflictException("Ya existe un usuario registrado con ese correo electrónico.");

        string passwordTemporal = "Estudiante" + Guid.NewGuid().ToString("N")[..6] + "!";

        var usuario = new Usuario
        {
            Nombre = request.Nombre,
            Correo = request.Correo,
            PasswordHash = _passwordHasher.Hash(passwordTemporal),
            Activo = true,
            DebeCambiarPassword = true,
            FechaRegistro = DateTime.UtcNow
        };
        await _context.AddAsync(usuario, ct);

        var estudiante = new Estudiante
        {
            UsuarioId = usuario.Id,
            UniversidadId = universidadId,
            RegistradoPorUsuarioId = registradoPorUsuarioId,
            Carrera = request.Carrera,
            DisponibilidadHoras = request.DisponibilidadHoras,
            Experiencia = request.Experiencia,
            Activo = true
        };
        await _context.AddAsync(estudiante, ct);

        await _context.SaveChangesAsync(ct);

        return new EstudianteCreadoResponse
        {
            EstudianteId = estudiante.Id,
            UsuarioId = usuario.Id,
            PasswordTemporal = passwordTemporal
        };
    }

    public Task<PagedResult<EstudianteResponse>> ListarPorUniversidadAsync(Guid universidadId, bool? activo, string? q, int pagina, int tamano, CancellationToken ct = default)
    {
        var query = _context.Estudiantes
            .Where(e => e.UniversidadId == universidadId);

        if (activo.HasValue)
            query = query.Where(e => e.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.ToLower();
            query = query.Where(e => e.Usuario.Nombre.ToLower().Contains(term) || e.Usuario.Correo.ToLower().Contains(term) || e.Carrera.ToLower().Contains(term));
        }

        int total = query.Count();
        var items = query
            .OrderBy(e => e.Usuario.Nombre)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(e => new EstudianteResponse
            {
                Id = e.Id,
                Nombre = e.Usuario.Nombre,
                Correo = e.Usuario.Correo,
                Carrera = e.Carrera,
                Activo = e.Activo
            }).ToList();

        return Task.FromResult(new PagedResult<EstudianteResponse>(items, total, pagina, tamano));
    }

    public async Task CambiarEstadoAsync(Guid universidadId, Guid estudianteId, bool activo, CancellationToken ct = default)
    {
        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.Id == estudianteId && e.UniversidadId == universidadId);
        if (estudiante == null)
            throw new NotFoundException("Estudiante no encontrado en su universidad.");

        estudiante.Activo = activo;
        if (!activo)
            estudiante.FechaDesactivacion = DateTime.UtcNow;
        else
            estudiante.FechaDesactivacion = null;

        await _context.SaveChangesAsync(ct);
    }

    public Task<PerfilEstudianteResponse> ObtenerMiPerfilAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var estudiante = _context.Estudiantes
            .FirstOrDefault(e => e.UsuarioId == usuarioId);

        if (estudiante == null)
            throw new NotFoundException("Perfil de estudiante no encontrado.");

        var habilidades = _context.EstudianteHabilidades
            .Where(eh => eh.EstudianteId == estudiante.Id)
            .Select(eh => new HabilidadEstudianteItemDto
            {
                HabilidadId = eh.HabilidadId,
                Nombre = eh.Habilidad.Nombre,
                Nivel = eh.Nivel
            }).ToList();

        var perfil = new PerfilEstudianteResponse
        {
            Id = estudiante.Id,
            Nombre = estudiante.Usuario.Nombre,
            Correo = estudiante.Usuario.Correo,
            Universidad = estudiante.Universidad.Nombre,
            Carrera = estudiante.Carrera,
            DisponibilidadHoras = estudiante.DisponibilidadHoras,
            Experiencia = estudiante.Experiencia,
            Activo = estudiante.Activo,
            Habilidades = habilidades
        };

        return Task.FromResult(perfil);
    }

    public async Task<PerfilEstudianteResponse> ActualizarMiPerfilAsync(Guid usuarioId, ActualizarPerfilRequest request, CancellationToken ct = default)
    {
        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.UsuarioId == usuarioId);
        if (estudiante == null)
            throw new NotFoundException("Perfil de estudiante no encontrado.");

        estudiante.Carrera = request.Carrera;
        estudiante.DisponibilidadHoras = request.DisponibilidadHoras;
        estudiante.Experiencia = request.Experiencia;

        await _context.SaveChangesAsync(ct);

        return await ObtenerMiPerfilAsync(usuarioId, ct);
    }
}

