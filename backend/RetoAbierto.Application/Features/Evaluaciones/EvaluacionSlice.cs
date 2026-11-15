using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Evaluaciones.DTOs;

public class CrearEvaluacionRequest
{
    public int Calificacion { get; set; }
    public string? Comentarios { get; set; }
}

public class EvaluacionResponse
{
    public Guid Id { get; set; }
    public Guid AsignacionId { get; set; }
    public string EvaluadoPor { get; set; } = string.Empty;
    public int Calificacion { get; set; }
    public string? Comentarios { get; set; }
    public string FechaEvaluacion { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Evaluaciones.Validators;

using RetoAbierto.Application.Features.Evaluaciones.DTOs;

public class CrearEvaluacionValidator : AbstractValidator<CrearEvaluacionRequest>
{
    public CrearEvaluacionValidator()
    {
        RuleFor(x => x.Calificacion)
            .InclusiveBetween(1, 5).WithMessage("La calificación debe ser un entero entre 1 y 5.");
    }
}

namespace RetoAbierto.Application.Features.Evaluaciones.Services;

using RetoAbierto.Application.Features.Evaluaciones.DTOs;

public interface IEvaluacionService
{
    Task<EvaluacionResponse> EvaluarAsync(Guid empresaId, Guid usuarioEvaluadorId, Guid asignacionId, CrearEvaluacionRequest request, CancellationToken ct = default);
    Task<List<EvaluacionResponse>> ListarPorAsignacionAsync(Guid asignacionId, CancellationToken ct = default);
}

public class EvaluacionService : IEvaluacionService
{
    private readonly IApplicationDbContext _context;

    public EvaluacionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluacionResponse> EvaluarAsync(Guid empresaId, Guid usuarioEvaluadorId, Guid asignacionId, CrearEvaluacionRequest request, CancellationToken ct = default)
    {
        var asignacion = _context.Asignaciones.FirstOrDefault(a => a.Id == asignacionId && a.EmpresaId == empresaId);
        if (asignacion == null)
            throw new NotFoundException("Asignación no encontrada.");

        if (asignacion.Estado != AsignacionEstado.Completada)
            throw new ConflictException("Solo se pueden evaluar asignaciones que se encuentren en estado Completada.");

        var evaluador = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioEvaluadorId);
        if (evaluador == null)
            throw new NotFoundException("Usuario evaluador no encontrado.");

        var evaluacion = new Evaluacion
        {
            EmpresaId = empresaId,
            AsignacionId = asignacionId,
            EvaluadoPorUsuarioId = usuarioEvaluadorId,
            Calificacion = request.Calificacion,
            Comentarios = request.Comentarios,
            FechaEvaluacion = DateTime.UtcNow
        };
        await _context.AddAsync(evaluacion, ct);
        await _context.SaveChangesAsync(ct);

        return new EvaluacionResponse
        {
            Id = evaluacion.Id,
            AsignacionId = asignacionId,
            EvaluadoPor = evaluador.Nombre,
            Calificacion = evaluacion.Calificacion,
            Comentarios = evaluacion.Comentarios,
            FechaEvaluacion = evaluacion.FechaEvaluacion.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public Task<List<EvaluacionResponse>> ListarPorAsignacionAsync(Guid asignacionId, CancellationToken ct = default)
    {
        var list = _context.Evaluaciones
            .Where(e => e.AsignacionId == asignacionId)
            .Select(e => new EvaluacionResponse
            {
                Id = e.Id,
                AsignacionId = e.AsignacionId,
                EvaluadoPor = e.EvaluadoPorUsuario.Nombre,
                Calificacion = e.Calificacion,
                Comentarios = e.Comentarios,
                FechaEvaluacion = e.FechaEvaluacion.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();

        return Task.FromResult(list);
    }
}

