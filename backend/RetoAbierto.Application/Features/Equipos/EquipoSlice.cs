using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Equipos.DTOs;

public class CrearEquipoRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public class AgregarIntegranteRequest
{
    public Guid EstudianteId { get; set; }
}

public class IntegranteResponseDto
{
    public Guid EstudianteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public bool EsLider { get; set; }
    public string FechaIngreso { get; set; } = string.Empty;
}

public class EquipoResponse
{
    public Guid Id { get; set; }
    public Guid RetoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public List<IntegranteResponseDto> Integrantes { get; set; } = new();
}

namespace RetoAbierto.Application.Features.Equipos.Validators;

using RetoAbierto.Application.Features.Equipos.DTOs;

public class CrearEquipoValidator : AbstractValidator<CrearEquipoRequest>
{
    public CrearEquipoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del equipo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe superar los 100 caracteres.");
    }
}

namespace RetoAbierto.Application.Features.Equipos.Services;

using RetoAbierto.Application.Features.Equipos.DTOs;

public interface IEquipoService
{
    Task<EquipoResponse> CrearEquipoAsync(Guid estudianteId, Guid retoId, CrearEquipoRequest request, CancellationToken ct = default);
    Task<List<EquipoResponse>> ListarPorRetoAsync(Guid retoId, CancellationToken ct = default);
    Task<EquipoResponse> ObtenerPorIdAsync(Guid equipoId, CancellationToken ct = default);
    Task<EquipoResponse> AgregarIntegranteAsync(Guid liderEstudianteId, Guid equipoId, Guid nuevoEstudianteId, CancellationToken ct = default);
    Task EliminarIntegranteAsync(Guid liderEstudianteId, Guid equipoId, Guid estudianteIdAEliminar, CancellationToken ct = default);
}

public class EquipoService : IEquipoService
{
    private readonly IApplicationDbContext _context;

    public EquipoService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EquipoResponse> CrearEquipoAsync(Guid estudianteId, Guid retoId, CrearEquipoRequest request, CancellationToken ct = default)
    {
        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.Id == estudianteId && e.Activo);
        if (estudiante == null)
            throw new NotFoundException("Estudiante no encontrado o inactivo.");

        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        if (reto.Estado != RetoEstado.Abierto)
            throw new ConflictException("El reto no está abierto.");

        var yaPertenece = _context.EquipoIntegrantes.Any(ei => ei.Equipo.RetoId == retoId && ei.EstudianteId == estudianteId);
        if (yaPertenece)
            throw new ConflictException("El estudiante ya pertenece a otro equipo para este mismo reto.");

        var equipo = new Equipo
        {
            EmpresaId = reto.EmpresaId,
            RetoId = retoId,
            Nombre = request.Nombre,
            Estado = EquipoEstado.Activo
        };
        await _context.AddAsync(equipo, ct);

        var integranteLider = new EquipoIntegrante
        {
            EmpresaId = reto.EmpresaId,
            EquipoId = equipo.Id,
            EstudianteId = estudianteId,
            EsLider = true,
            FechaIngreso = DateTime.UtcNow
        };
        await _context.AddAsync(integranteLider, ct);

        await _context.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(equipo.Id, ct);
    }

    public Task<List<EquipoResponse>> ListarPorRetoAsync(Guid retoId, CancellationToken ct = default)
    {
        var equipos = _context.Equipos
            .Where(e => e.RetoId == retoId)
            .Select(e => new EquipoResponse
            {
                Id = e.Id,
                RetoId = e.RetoId,
                Nombre = e.Nombre,
                Estado = e.Estado.ToString(),
                Integrantes = e.Integrantes.Select(i => new IntegranteResponseDto
                {
                    EstudianteId = i.EstudianteId,
                    Nombre = i.Estudiante.Usuario.Nombre,
                    Carrera = i.Estudiante.Carrera,
                    EsLider = i.EsLider,
                    FechaIngreso = i.FechaIngreso.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToList()
            }).ToList();

        return Task.FromResult(equipos);
    }

    public Task<EquipoResponse> ObtenerPorIdAsync(Guid equipoId, CancellationToken ct = default)
    {
        var equipo = _context.Equipos.FirstOrDefault(e => e.Id == equipoId);
        if (equipo == null)
            throw new NotFoundException("Equipo no encontrado.");

        var integrantes = _context.EquipoIntegrantes
            .Where(ei => ei.EquipoId == equipo.Id)
            .Select(i => new IntegranteResponseDto
            {
                EstudianteId = i.EstudianteId,
                Nombre = i.Estudiante.Usuario.Nombre,
                Carrera = i.Estudiante.Carrera,
                EsLider = i.EsLider,
                FechaIngreso = i.FechaIngreso.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();

        return Task.FromResult(new EquipoResponse
        {
            Id = equipo.Id,
            RetoId = equipo.RetoId,
            Nombre = equipo.Nombre,
            Estado = equipo.Estado.ToString(),
            Integrantes = integrantes
        });
    }

    public async Task<EquipoResponse> AgregarIntegranteAsync(Guid liderEstudianteId, Guid equipoId, Guid nuevoEstudianteId, CancellationToken ct = default)
    {
        var equipo = _context.Equipos.FirstOrDefault(e => e.Id == equipoId);
        if (equipo == null)
            throw new NotFoundException("Equipo no encontrado.");

        var esLider = _context.EquipoIntegrantes.Any(ei => ei.EquipoId == equipoId && ei.EstudianteId == liderEstudianteId && ei.EsLider);
        if (!esLider)
            throw new ForbiddenException("Solo el líder del equipo puede agregar integrantes.");

        var reto = _context.Retos.FirstOrDefault(r => r.Id == equipo.RetoId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        if (reto.Estado != RetoEstado.Abierto)
            throw new ConflictException("El reto no está abierto.");

        int cantidadActual = _context.EquipoIntegrantes.Count(ei => ei.EquipoId == equipoId);
        if (cantidadActual >= reto.MaxIntegrantes)
            throw new ConflictException("El equipo supera el máximo de integrantes permitido por el reto.");

        var nuevoEstudiante = _context.Estudiantes.FirstOrDefault(e => e.Id == nuevoEstudianteId && e.Activo);
        if (nuevoEstudiante == null)
            throw new NotFoundException("El estudiante a agregar no existe o está inactivo.");

        var yaEnEquipo = _context.EquipoIntegrantes.Any(ei => ei.Equipo.RetoId == equipo.RetoId && ei.EstudianteId == nuevoEstudianteId);
        if (yaEnEquipo)
            throw new ConflictException("El estudiante ya pertenece a un equipo en este reto.");

        var nuevoIntegrante = new EquipoIntegrante
        {
            EmpresaId = equipo.EmpresaId,
            EquipoId = equipo.Id,
            EstudianteId = nuevoEstudianteId,
            EsLider = false,
            FechaIngreso = DateTime.UtcNow
        };
        await _context.AddAsync(nuevoIntegrante, ct);
        await _context.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(equipoId, ct);
    }

    public async Task EliminarIntegranteAsync(Guid liderEstudianteId, Guid equipoId, Guid estudianteIdAEliminar, CancellationToken ct = default)
    {
        var equipo = _context.Equipos.FirstOrDefault(e => e.Id == equipoId);
        if (equipo == null)
            throw new NotFoundException("Equipo no encontrado.");

        var esLider = _context.EquipoIntegrantes.Any(ei => ei.EquipoId == equipoId && ei.EstudianteId == liderEstudianteId && ei.EsLider);
        if (!esLider)
            throw new ForbiddenException("Solo el líder puede retirar integrantes.");

        var integrante = _context.EquipoIntegrantes.FirstOrDefault(ei => ei.EquipoId == equipoId && ei.EstudianteId == estudianteIdAEliminar);
        if (integrante == null)
            throw new NotFoundException("El estudiante no forma parte de este equipo.");

        if (integrante.EsLider)
            throw new ConflictException("Se intenta eliminar al líder sin transferir previamente el liderazgo.");

        _context.Remove(integrante);
        await _context.SaveChangesAsync(ct);
    }
}

