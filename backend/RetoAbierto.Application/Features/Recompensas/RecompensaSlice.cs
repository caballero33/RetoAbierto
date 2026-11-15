using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Recompensas.DTOs;

public class CrearRecompensaRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public decimal Valor { get; set; }
}

public class RecompensaResponse
{
    public Guid Id { get; set; }
    public Guid RetoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public decimal Valor { get; set; }
}

public class EntregarRecompensaRequest
{
    public Guid EstudianteId { get; set; }
}

public class RecompensaEntregaResponse
{
    public Guid RecompensaId { get; set; }
    public Guid EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = string.Empty;
    public string FechaEntrega { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class CambiarEstadoEntregaRequest
{
    public string Estado { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Recompensas.Validators;

using RetoAbierto.Application.Features.Recompensas.DTOs;

public class CrearRecompensaValidator : AbstractValidator<CrearRecompensaRequest>
{
    public CrearRecompensaValidator()
    {
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.");

        RuleFor(x => x.Tipo)
            .MaximumLength(50).WithMessage("El tipo no debe superar los 50 caracteres.");

        RuleFor(x => x.Valor)
            .GreaterThanOrEqualTo(0).WithMessage("El valor debe ser mayor o igual a cero.");
    }
}

namespace RetoAbierto.Application.Features.Recompensas.Services;

using RetoAbierto.Application.Features.Recompensas.DTOs;

public interface IRecompensaService
{
    Task<List<RecompensaResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, CancellationToken ct = default);
    Task<RecompensaResponse> CrearAsync(Guid empresaId, Guid retoId, CrearRecompensaRequest request, CancellationToken ct = default);
    Task<RecompensaEntregaResponse> RegistrarEntregaAsync(Guid empresaId, Guid recompensaId, Guid estudianteId, CancellationToken ct = default);
    Task CambiarEstadoEntregaAsync(Guid empresaId, Guid recompensaId, Guid estudianteId, string nuevoEstadoStr, CancellationToken ct = default);
}

public class RecompensaService : IRecompensaService
{
    private readonly IApplicationDbContext _context;

    public RecompensaService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<RecompensaResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, CancellationToken ct = default)
    {
        var recompensas = _context.Recompensas
            .Where(r => r.EmpresaId == empresaId && r.RetoId == retoId)
            .Select(r => new RecompensaResponse
            {
                Id = r.Id,
                RetoId = r.RetoId,
                Descripcion = r.Descripcion,
                Tipo = r.Tipo,
                Valor = r.Valor
            }).ToList();

        return Task.FromResult(recompensas);
    }

    public async Task<RecompensaResponse> CrearAsync(Guid empresaId, Guid retoId, CrearRecompensaRequest request, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        var recompensa = new Recompensa
        {
            EmpresaId = empresaId,
            RetoId = retoId,
            Descripcion = request.Descripcion,
            Tipo = request.Tipo,
            Valor = request.Valor
        };
        await _context.AddAsync(recompensa, ct);
        await _context.SaveChangesAsync(ct);

        return new RecompensaResponse
        {
            Id = recompensa.Id,
            RetoId = recompensa.RetoId,
            Descripcion = recompensa.Descripcion,
            Tipo = recompensa.Tipo,
            Valor = recompensa.Valor
        };
    }

    public async Task<RecompensaEntregaResponse> RegistrarEntregaAsync(Guid empresaId, Guid recompensaId, Guid estudianteId, CancellationToken ct = default)
    {
        var recompensa = _context.Recompensas.FirstOrDefault(r => r.Id == recompensaId && r.EmpresaId == empresaId);
        if (recompensa == null)
            throw new NotFoundException("Recompensa no encontrada.");

        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.Id == estudianteId);
        if (estudiante == null)
            throw new NotFoundException("Estudiante no encontrado.");

        var participoEnAsignacion = _context.Asignaciones.Any(a =>
            a.EmpresaId == empresaId &&
            a.Postulacion.RetoId == recompensa.RetoId &&
            a.Postulacion.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId));

        if (!participoEnAsignacion)
            throw new ConflictException("El estudiante no pertenece a un equipo con asignación para este reto.");

        var entregaExistente = _context.RecompensaEntregas
            .FirstOrDefault(re => re.RecompensaId == recompensaId && re.EstudianteId == estudianteId);

        if (entregaExistente != null)
            throw new ConflictException("Ya existe un registro de entrega de esta recompensa para el estudiante.");

        var entrega = new RecompensaEntrega
        {
            EmpresaId = empresaId,
            RecompensaId = recompensaId,
            EstudianteId = estudianteId,
            FechaEntrega = DateTime.UtcNow,
            Estado = RecompensaEntregaEstado.Pendiente
        };
        await _context.AddAsync(entrega, ct);
        await _context.SaveChangesAsync(ct);

        return new RecompensaEntregaResponse
        {
            RecompensaId = recompensaId,
            EstudianteId = estudianteId,
            EstudianteNombre = estudiante.Usuario.Nombre,
            FechaEntrega = entrega.FechaEntrega.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Estado = entrega.Estado.ToString()
        };
    }

    public async Task CambiarEstadoEntregaAsync(Guid empresaId, Guid recompensaId, Guid estudianteId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RecompensaEntregaEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado inválido. Valores: Pendiente, Entregada.");

        var entrega = _context.RecompensaEntregas
            .FirstOrDefault(re => re.RecompensaId == recompensaId && re.EstudianteId == estudianteId && re.EmpresaId == empresaId);

        if (entrega == null)
            throw new NotFoundException("Registro de entrega no encontrado.");

        entrega.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
    }
}

