using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Asignaciones.DTOs;

public class CrearAsignacionRequest
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class AsignacionResponse
{
    public Guid Id { get; set; }
    public Guid PostulacionId { get; set; }
    public Guid RetoId { get; set; }
    public string RetoTitulo { get; set; } = string.Empty;
    public string EquipoNombre { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int HorasRegistradas { get; set; }
}

public class CambiarEstadoAsignacionRequest
{
    public string Estado { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Asignaciones.Validators;

using RetoAbierto.Application.Features.Asignaciones.DTOs;

public class CrearAsignacionValidator : AbstractValidator<CrearAsignacionRequest>
{
    public CrearAsignacionValidator()
    {
        RuleFor(x => x.FechaFin)
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha fin debe ser igual o posterior a la fecha de inicio.");
    }
}

public class CambiarEstadoAsignacionValidator : AbstractValidator<CambiarEstadoAsignacionRequest>
{
    public CambiarEstadoAsignacionValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(e => Enum.TryParse<AsignacionEstado>(e, true, out _))
            .WithMessage("Estados permitidos: EnCurso, Completada, Cancelada.");
    }
}

namespace RetoAbierto.Application.Features.Asignaciones.Services;

using RetoAbierto.Application.Features.Asignaciones.DTOs;

public interface IAsignacionService
{
    Task<AsignacionResponse> CrearAsync(Guid empresaId, Guid postulacionId, CrearAsignacionRequest request, CancellationToken ct = default);
    Task<List<AsignacionResponse>> ListarAsync(Guid? empresaId, Guid? estudianteId, string? estado, CancellationToken ct = default);
    Task<AsignacionResponse> ObtenerPorIdAsync(Guid asignacionId, Guid? empresaId, Guid? estudianteId, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid empresaId, Guid asignacionId, string nuevoEstadoStr, CancellationToken ct = default);
}

public class AsignacionService : IAsignacionService
{
    private readonly IApplicationDbContext _context;

    public AsignacionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AsignacionResponse> CrearAsync(Guid empresaId, Guid postulacionId, CrearAsignacionRequest request, CancellationToken ct = default)
    {
        var postulacion = _context.Postulaciones
            .FirstOrDefault(p => p.Id == postulacionId && p.EmpresaId == empresaId);

        if (postulacion == null)
            throw new NotFoundException("Postulación no encontrada.");

        if (postulacion.Estado != PostulacionEstado.Aceptada)
            throw new ConflictException("La postulación debe encontrarse en estado Aceptada para generar una asignación.");

        var yaExiste = _context.Asignaciones.Any(a => a.PostulacionId == postulacionId);
        if (yaExiste)
            throw new ConflictException("Ya existe una asignación generada para esta postulación.");

        var asignacion = new Asignacion
        {
            EmpresaId = empresaId,
            PostulacionId = postulacionId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Estado = AsignacionEstado.EnCurso
        };
        await _context.AddAsync(asignacion, ct);

        var reto = _context.Retos.FirstOrDefault(r => r.Id == postulacion.RetoId);
        if (reto != null && reto.Estado == RetoEstado.Abierto)
        {
            reto.Estado = RetoEstado.EnCurso;
        }

        await _context.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(asignacion.Id, empresaId, null, ct);
    }

    public Task<List<AsignacionResponse>> ListarAsync(Guid? empresaId, Guid? estudianteId, string? estado, CancellationToken ct = default)
    {
        var query = _context.Asignaciones.AsQueryable();

        if (empresaId.HasValue)
        {
            query = query.Where(a => a.EmpresaId == empresaId.Value);
        }
        else if (estudianteId.HasValue)
        {
            query = query.Where(a => a.Postulacion.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId.Value));
        }

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<AsignacionEstado>(estado, true, out var est))
            query = query.Where(a => a.Estado == est);

        var list = query.Select(a => new AsignacionResponse
        {
            Id = a.Id,
            PostulacionId = a.PostulacionId,
            RetoId = a.Postulacion.RetoId,
            RetoTitulo = a.Postulacion.Reto.Titulo,
            EquipoNombre = a.Postulacion.Equipo.Nombre,
            FechaInicio = a.FechaInicio.ToString("yyyy-MM-dd"),
            FechaFin = a.FechaFin.ToString("yyyy-MM-dd"),
            Estado = a.Estado.ToString(),
            HorasRegistradas = a.RegistrosHoras.Sum(h => h.Horas)
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<AsignacionResponse> ObtenerPorIdAsync(Guid asignacionId, Guid? empresaId, Guid? estudianteId, CancellationToken ct = default)
    {
        var a = _context.Asignaciones.FirstOrDefault(x => x.Id == asignacionId);
        if (a == null)
            throw new NotFoundException("Asignación no encontrada.");

        if (empresaId.HasValue && a.EmpresaId != empresaId.Value)
            throw new ForbiddenException("No tiene acceso a las asignaciones de otra empresa.");

        if (estudianteId.HasValue && !a.Postulacion.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId.Value))
            throw new ForbiddenException("El estudiante no participa en esta asignación.");

        int horas = _context.RegistroHoras.Where(r => r.AsignacionId == a.Id).Sum(h => (int?)h.Horas) ?? 0;

        return Task.FromResult(new AsignacionResponse
        {
            Id = a.Id,
            PostulacionId = a.PostulacionId,
            RetoId = a.Postulacion.RetoId,
            RetoTitulo = a.Postulacion.Reto.Titulo,
            EquipoNombre = a.Postulacion.Equipo.Nombre,
            FechaInicio = a.FechaInicio.ToString("yyyy-MM-dd"),
            FechaFin = a.FechaFin.ToString("yyyy-MM-dd"),
            Estado = a.Estado.ToString(),
            HorasRegistradas = horas
        });
    }

    public async Task CambiarEstadoAsync(Guid empresaId, Guid asignacionId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AsignacionEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado de asignación inválido.");

        var asignacion = _context.Asignaciones.FirstOrDefault(a => a.Id == asignacionId && a.EmpresaId == empresaId);
        if (asignacion == null)
            throw new NotFoundException("Asignación no encontrada.");

        asignacion.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
    }
}

