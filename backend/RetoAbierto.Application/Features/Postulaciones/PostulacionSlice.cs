using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Postulaciones.DTOs;

public class CrearPostulacionRequest
{
    public Guid EquipoId { get; set; }
    public string Propuesta { get; set; } = string.Empty;
}

public class EquipoResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class PostulacionResponse
{
    public Guid Id { get; set; }
    public Guid RetoId { get; set; }
    public string RetoTitulo { get; set; } = string.Empty;
    public EquipoResumenDto Equipo { get; set; } = new();
    public string Propuesta { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string FechaPostulacion { get; set; } = string.Empty;
}

public class CambiarEstadoPostulacionRequest
{
    public string Estado { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Postulaciones.Validators;

using RetoAbierto.Application.Features.Postulaciones.DTOs;

public class CrearPostulacionValidator : AbstractValidator<CrearPostulacionRequest>
{
    public CrearPostulacionValidator()
    {
        RuleFor(x => x.EquipoId)
            .NotEmpty().WithMessage("El identificador del equipo es obligatorio.");

        RuleFor(x => x.Propuesta)
            .NotEmpty().WithMessage("La propuesta es obligatoria.")
            .MinimumLength(50).WithMessage("La propuesta debe tener un mínimo de 50 caracteres.");
    }
}

namespace RetoAbierto.Application.Features.Postulaciones.Services;

using RetoAbierto.Application.Features.Postulaciones.DTOs;

public interface IPostulacionService
{
    Task<PostulacionResponse> PostularAsync(Guid estudianteId, Guid retoId, CrearPostulacionRequest request, CancellationToken ct = default);
    Task<List<PostulacionResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, string? estado, CancellationToken ct = default);
    Task<List<PostulacionResponse>> ListarMiasAsync(Guid estudianteId, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid empresaId, Guid postulacionId, string nuevoEstadoStr, CancellationToken ct = default);
    Task RetirarAsync(Guid estudianteId, Guid postulacionId, CancellationToken ct = default);
}

public class PostulacionService : IPostulacionService
{
    private readonly IApplicationDbContext _context;

    public PostulacionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostulacionResponse> PostularAsync(Guid estudianteId, Guid retoId, CrearPostulacionRequest request, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        if (reto.Estado != RetoEstado.Abierto)
            throw new ConflictException("El reto no está abierto.");

        var equipo = _context.Equipos.FirstOrDefault(e => e.Id == request.EquipoId && e.RetoId == retoId);
        if (equipo == null)
            throw new NotFoundException("El equipo especificado no pertenece a este reto.");

        var esLider = _context.EquipoIntegrantes.Any(ei => ei.EquipoId == equipo.Id && ei.EstudianteId == estudianteId && ei.EsLider);
        if (!esLider)
            throw new ForbiddenException("El usuario autenticado debe ser el líder del equipo para postular.");

        var yaPostulo = _context.Postulaciones.Any(p => p.RetoId == retoId && p.EquipoId == equipo.Id);
        if (yaPostulo)
            throw new ConflictException("El equipo ya postuló al reto.");

        var postulacion = new Postulacion
        {
            EmpresaId = reto.EmpresaId,
            RetoId = retoId,
            EquipoId = equipo.Id,
            Propuesta = request.Propuesta,
            Estado = PostulacionEstado.Enviada,
            FechaPostulacion = DateTime.UtcNow
        };
        await _context.AddAsync(postulacion, ct);
        await _context.SaveChangesAsync(ct);

        return new PostulacionResponse
        {
            Id = postulacion.Id,
            RetoId = reto.Id,
            RetoTitulo = reto.Titulo,
            Equipo = new EquipoResumenDto { Id = equipo.Id, Nombre = equipo.Nombre },
            Propuesta = postulacion.Propuesta,
            Estado = postulacion.Estado.ToString(),
            FechaPostulacion = postulacion.FechaPostulacion.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public Task<List<PostulacionResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, string? estado, CancellationToken ct = default)
    {
        var query = _context.Postulaciones
            .Where(p => p.EmpresaId == empresaId && p.RetoId == retoId);

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<PostulacionEstado>(estado, true, out var est))
            query = query.Where(p => p.Estado == est);

        var list = query.Select(p => new PostulacionResponse
        {
            Id = p.Id,
            RetoId = p.RetoId,
            RetoTitulo = p.Reto.Titulo,
            Equipo = new EquipoResumenDto { Id = p.Equipo.Id, Nombre = p.Equipo.Nombre },
            Propuesta = p.Propuesta,
            Estado = p.Estado.ToString(),
            FechaPostulacion = p.FechaPostulacion.ToString("yyyy-MM-ddTHH:mm:ssZ")
        }).ToList();

        return Task.FromResult(list);
    }

    public Task<List<PostulacionResponse>> ListarMiasAsync(Guid estudianteId, CancellationToken ct = default)
    {
        var list = _context.Postulaciones
            .Where(p => p.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId))
            .Select(p => new PostulacionResponse
            {
                Id = p.Id,
                RetoId = p.RetoId,
                RetoTitulo = p.Reto.Titulo,
                Equipo = new EquipoResumenDto { Id = p.Equipo.Id, Nombre = p.Equipo.Nombre },
                Propuesta = p.Propuesta,
                Estado = p.Estado.ToString(),
                FechaPostulacion = p.FechaPostulacion.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }).ToList();

        return Task.FromResult(list);
    }

    public async Task CambiarEstadoAsync(Guid empresaId, Guid postulacionId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PostulacionEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado de postulación inválido.");

        var post = _context.Postulaciones.FirstOrDefault(p => p.Id == postulacionId && p.EmpresaId == empresaId);
        if (post == null)
            throw new NotFoundException("Postulación no encontrada.");

        post.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
    }

    public async Task RetirarAsync(Guid estudianteId, Guid postulacionId, CancellationToken ct = default)
    {
        var post = _context.Postulaciones.FirstOrDefault(p => p.Id == postulacionId);
        if (post == null)
            throw new NotFoundException("Postulación no encontrada.");

        var esLider = _context.EquipoIntegrantes.Any(ei => ei.EquipoId == post.EquipoId && ei.EstudianteId == estudianteId && ei.EsLider);
        if (!esLider)
            throw new ForbiddenException("Solo el líder del equipo puede retirar la postulación.");

        if (post.Estado == PostulacionEstado.Aceptada)
            throw new ConflictException("No se puede retirar una postulación que ya ha sido Aceptada.");

        post.Estado = PostulacionEstado.Retirada;
        await _context.SaveChangesAsync(ct);
    }
}

