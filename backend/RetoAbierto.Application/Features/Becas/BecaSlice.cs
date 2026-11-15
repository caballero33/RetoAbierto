using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Becas.DTOs;

public class CrearBecaRequest
{
    public Guid EstudianteId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
}

public class BecaResponse
{
    public Guid Id { get; set; }
    public Guid EstudianteId { get; set; }
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? ValidadaPor { get; set; }
    public string? FechaValidacion { get; set; }
}

public class CambiarEstadoBecaRequest
{
    public string Estado { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Becas.Validators;

using RetoAbierto.Application.Features.Becas.DTOs;

public class CrearBecaValidator : AbstractValidator<CrearBecaRequest>
{
    public CrearBecaValidator()
    {
        RuleFor(x => x.EstudianteId)
            .NotEmpty().WithMessage("El identificador del estudiante es obligatorio.");

        RuleFor(x => x.FechaFin)
            .GreaterThanOrEqualTo(x => x.FechaInicio)
            .WithMessage("La fecha fin debe ser igual o posterior a la fecha de inicio.");
    }
}

public class CambiarEstadoBecaValidator : AbstractValidator<CambiarEstadoBecaRequest>
{
    public CambiarEstadoBecaValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(e => Enum.TryParse<BecaEstado>(e, true, out _))
            .WithMessage("Estado inválido. Valores permitidos: Pendiente, Validada, Rechazada, Finalizada.");
    }
}

namespace RetoAbierto.Application.Features.Becas.Services;

using RetoAbierto.Application.Features.Becas.DTOs;

public interface IBecaService
{
    Task<BecaResponse> CrearAsync(Guid universidadId, CrearBecaRequest request, CancellationToken ct = default);
    Task<List<BecaResponse>> ListarAsync(Guid? universidadId, Guid? estudianteId, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid universidadId, Guid usuarioId, Guid becaId, string nuevoEstadoStr, CancellationToken ct = default);
}

public class BecaService : IBecaService
{
    private readonly IApplicationDbContext _context;

    public BecaService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BecaResponse> CrearAsync(Guid universidadId, CrearBecaRequest request, CancellationToken ct = default)
    {
        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.Id == request.EstudianteId && e.UniversidadId == universidadId);
        if (estudiante == null)
            throw new NotFoundException("Estudiante no encontrado en su universidad.");

        var beca = new Beca
        {
            EstudianteId = request.EstudianteId,
            FechaInicio = request.FechaInicio,
            FechaFin = request.FechaFin,
            Estado = BecaEstado.Pendiente
        };
        await _context.AddAsync(beca, ct);
        await _context.SaveChangesAsync(ct);

        return new BecaResponse
        {
            Id = beca.Id,
            EstudianteId = beca.EstudianteId,
            FechaInicio = beca.FechaInicio.ToString("yyyy-MM-dd"),
            FechaFin = beca.FechaFin.ToString("yyyy-MM-dd"),
            Estado = beca.Estado.ToString(),
            ValidadaPor = null,
            FechaValidacion = null
        };
    }

    public Task<List<BecaResponse>> ListarAsync(Guid? universidadId, Guid? estudianteId, CancellationToken ct = default)
    {
        var query = _context.Becas.AsQueryable();

        if (universidadId.HasValue)
        {
            query = query.Where(b => b.Estudiante.UniversidadId == universidadId.Value);
        }
        else if (estudianteId.HasValue)
        {
            query = query.Where(b => b.EstudianteId == estudianteId.Value);
        }
        else
        {
            return Task.FromResult(new List<BecaResponse>());
        }

        var lista = query.Select(b => new BecaResponse
        {
            Id = b.Id,
            EstudianteId = b.EstudianteId,
            FechaInicio = b.FechaInicio.ToString("yyyy-MM-dd"),
            FechaFin = b.FechaFin.ToString("yyyy-MM-dd"),
            Estado = b.Estado.ToString(),
            ValidadaPor = b.ValidadaPorUsuario != null ? b.ValidadaPorUsuario.Nombre : null,
            FechaValidacion = b.FechaValidacion.HasValue ? b.FechaValidacion.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : null
        }).ToList();

        return Task.FromResult(lista);
    }

    public async Task CambiarEstadoAsync(Guid universidadId, Guid usuarioId, Guid becaId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<BecaEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado inválido.");

        var beca = _context.Becas.FirstOrDefault(b => b.Id == becaId && b.Estudiante.UniversidadId == universidadId);
        if (beca == null)
            throw new NotFoundException("Beca no encontrada en su universidad.");

        beca.Estado = nuevoEstado;
        if (nuevoEstado == BecaEstado.Validada)
        {
            beca.ValidadaPorUsuarioId = usuarioId;
            beca.FechaValidacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }
}

