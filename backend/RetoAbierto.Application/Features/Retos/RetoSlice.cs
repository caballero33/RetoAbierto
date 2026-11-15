using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Common.Models;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Retos.DTOs;

public class HabilidadRequeridaDto
{
    public Guid HabilidadId { get; set; }
    public int NivelRequerido { get; set; }
}

public class HabilidadRequeridaDetalleDto
{
    public Guid HabilidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int NivelRequerido { get; set; }
}

public class CrearRetoRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Entregables { get; set; } = string.Empty;
    public int MaxIntegrantes { get; set; }
    public int HorasEstimadas { get; set; }
    public DateTime FechaLimite { get; set; }
    public List<HabilidadRequeridaDto> Habilidades { get; set; } = new();
}

public class ActualizarRetoRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Entregables { get; set; } = string.Empty;
    public int MaxIntegrantes { get; set; }
    public int HorasEstimadas { get; set; }
    public DateTime FechaLimite { get; set; }
}

public class CambiarEstadoRetoRequest
{
    public string Estado { get; set; } = string.Empty;
}

public class RetoResumenResponse
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string EmpresaNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string FechaLimite { get; set; } = string.Empty;
    public int MaxIntegrantes { get; set; }
}

public class RetoResponse
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Entregables { get; set; } = string.Empty;
    public int MaxIntegrantes { get; set; }
    public int HorasEstimadas { get; set; }
    public string FechaLimite { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string FechaCreacion { get; set; } = string.Empty;
    public string EmpresaNombre { get; set; } = string.Empty;
    public List<HabilidadRequeridaDetalleDto> Habilidades { get; set; } = new();
}

namespace RetoAbierto.Application.Features.Retos.Validators;

using RetoAbierto.Application.Features.Retos.DTOs;

public class CrearRetoValidator : AbstractValidator<CrearRetoRequest>
{
    public CrearRetoValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(150).WithMessage("El título no debe superar los 150 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.");

        RuleFor(x => x.Entregables)
            .NotEmpty().WithMessage("Los entregables son obligatorios.");

        RuleFor(x => x.MaxIntegrantes)
            .GreaterThan(0).WithMessage("El número máximo de integrantes debe ser mayor que cero.");

        RuleFor(x => x.HorasEstimadas)
            .GreaterThan(0).WithMessage("Las horas estimadas deben ser mayor que cero.");

        RuleFor(x => x.FechaLimite)
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha límite debe ser una fecha futura.");

        RuleFor(x => x.Habilidades)
            .NotEmpty().WithMessage("Debe contener al menos una habilidad.")
            .Must(h => h.Select(x => x.HabilidadId).Distinct().Count() == h.Count)
            .WithMessage("No se permiten habilidades repetidas.");

        RuleForEach(x => x.Habilidades).ChildRules(h =>
        {
            h.RuleFor(x => x.NivelRequerido)
                .InclusiveBetween(1, 5).WithMessage("El nivel requerido debe estar entre 1 y 5.");
        });
    }
}

public class ActualizarRetoValidator : AbstractValidator<ActualizarRetoRequest>
{
    public ActualizarRetoValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(150).WithMessage("El título no debe superar los 150 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.");

        RuleFor(x => x.Entregables)
            .NotEmpty().WithMessage("Los entregables son obligatorios.");

        RuleFor(x => x.MaxIntegrantes)
            .GreaterThan(0).WithMessage("El número máximo de integrantes debe ser mayor que cero.");

        RuleFor(x => x.HorasEstimadas)
            .GreaterThan(0).WithMessage("Las horas estimadas deben ser mayor que cero.");
    }
}

namespace RetoAbierto.Application.Features.Retos.Services;

using RetoAbierto.Application.Features.Retos.DTOs;

public interface IRetoService
{
    Task<PagedResult<RetoResumenResponse>> ListarAsync(Guid? empresaId, string? rol, string? estado, Guid? habilidadId, string? q, int pagina, int tamano, CancellationToken ct = default);
    Task<RetoResponse> ObtenerPorIdAsync(Guid retoId, Guid? empresaId, string? rol, CancellationToken ct = default);
    Task<RetoResponse> CrearAsync(Guid empresaId, CrearRetoRequest request, CancellationToken ct = default);
    Task<RetoResponse> ActualizarAsync(Guid empresaId, Guid retoId, ActualizarRetoRequest request, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid empresaId, Guid retoId, string nuevoEstadoStr, CancellationToken ct = default);
    Task EliminarAsync(Guid empresaId, Guid retoId, CancellationToken ct = default);
}

public class RetoService : IRetoService
{
    private readonly IApplicationDbContext _context;

    public RetoService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<PagedResult<RetoResumenResponse>> ListarAsync(Guid? empresaId, string? rol, string? estado, Guid? habilidadId, string? q, int pagina, int tamano, CancellationToken ct = default)
    {
        var query = _context.Retos.AsQueryable();

        if (rol == RolesConstantes.Estudiante)
        {
            query = query.Where(r => r.Estado == RetoEstado.Abierto);
        }
        else if (empresaId.HasValue)
        {
            query = query.Where(r => r.EmpresaId == empresaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<RetoEstado>(estado, true, out var est))
        {
            query = query.Where(r => r.Estado == est);
        }

        if (habilidadId.HasValue)
        {
            query = query.Where(r => r.Habilidades.Any(h => h.HabilidadId == habilidadId.Value));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.ToLower();
            query = query.Where(r => r.Titulo.ToLower().Contains(term) || r.Descripcion.ToLower().Contains(term));
        }

        int total = query.Count();
        var items = query
            .OrderByDescending(r => r.FechaCreacion)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .Select(r => new RetoResumenResponse
            {
                Id = r.Id,
                Titulo = r.Titulo,
                EmpresaNombre = r.Empresa.Nombre,
                Estado = r.Estado.ToString(),
                FechaLimite = r.FechaLimite.ToString("yyyy-MM-dd"),
                MaxIntegrantes = r.MaxIntegrantes
            }).ToList();

        return Task.FromResult(new PagedResult<RetoResumenResponse>(items, total, pagina, tamano));
    }

    public Task<RetoResponse> ObtenerPorIdAsync(Guid retoId, Guid? empresaId, string? rol, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        if (rol == RolesConstantes.Estudiante && reto.Estado != RetoEstado.Abierto)
            throw new ForbiddenException("El estudiante solo puede visualizar retos en estado Abierto.");

        if (empresaId.HasValue && reto.EmpresaId != empresaId.Value && rol != RolesConstantes.AdminPlataforma && rol != RolesConstantes.Estudiante)
            throw new ForbiddenException("No tiene permiso para visualizar este reto.");

        var habilidades = _context.RetoHabilidades
            .Where(rh => rh.RetoId == reto.Id)
            .Select(rh => new HabilidadRequeridaDetalleDto
            {
                HabilidadId = rh.HabilidadId,
                Nombre = rh.Habilidad.Nombre,
                NivelRequerido = rh.NivelRequerido
            }).ToList();

        return Task.FromResult(new RetoResponse
        {
            Id = reto.Id,
            Titulo = reto.Titulo,
            Descripcion = reto.Descripcion,
            Entregables = reto.Entregables,
            MaxIntegrantes = reto.MaxIntegrantes,
            HorasEstimadas = reto.HorasEstimadas,
            FechaLimite = reto.FechaLimite.ToString("yyyy-MM-dd"),
            Estado = reto.Estado.ToString(),
            FechaCreacion = reto.FechaCreacion.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            EmpresaNombre = reto.Empresa.Nombre,
            Habilidades = habilidades
        });
    }

    public async Task<RetoResponse> CrearAsync(Guid empresaId, CrearRetoRequest request, CancellationToken ct = default)
    {
        var empresa = _context.Empresas.FirstOrDefault(e => e.Id == empresaId);
        if (empresa == null)
            throw new NotFoundException("Empresa no encontrada.");

        var reto = new Reto
        {
            EmpresaId = empresaId,
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            Entregables = request.Entregables,
            MaxIntegrantes = request.MaxIntegrantes,
            HorasEstimadas = request.HorasEstimadas,
            FechaLimite = request.FechaLimite,
            Estado = RetoEstado.Borrador,
            FechaCreacion = DateTime.UtcNow
        };
        await _context.AddAsync(reto, ct);

        foreach (var hab in request.Habilidades)
        {
            var h = _context.Habilidades.FirstOrDefault(x => x.Id == hab.HabilidadId);
            if (h == null)
                throw new NotFoundException($"Habilidad {hab.HabilidadId} no existe en el catálogo.");

            var retoHab = new RetoHabilidad
            {
                EmpresaId = empresaId,
                RetoId = reto.Id,
                HabilidadId = hab.HabilidadId,
                NivelRequerido = hab.NivelRequerido
            };
            await _context.AddAsync(retoHab, ct);
        }

        await _context.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(reto.Id, empresaId, RolesConstantes.AdminEmpresa, ct);
    }

    public async Task<RetoResponse> ActualizarAsync(Guid empresaId, Guid retoId, ActualizarRetoRequest request, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        reto.Titulo = request.Titulo;
        reto.Descripcion = request.Descripcion;
        reto.Entregables = request.Entregables;
        reto.MaxIntegrantes = request.MaxIntegrantes;
        reto.HorasEstimadas = request.HorasEstimadas;
        reto.FechaLimite = request.FechaLimite;

        await _context.SaveChangesAsync(ct);

        return await ObtenerPorIdAsync(retoId, empresaId, RolesConstantes.AdminEmpresa, ct);
    }

    public async Task CambiarEstadoAsync(Guid empresaId, Guid retoId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<RetoEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado inválido.");

        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        bool esValido = false;
        switch (reto.Estado)
        {
            case RetoEstado.Borrador:
                esValido = nuevoEstado == RetoEstado.Abierto || nuevoEstado == RetoEstado.Cancelado;
                break;
            case RetoEstado.Abierto:
                esValido = nuevoEstado == RetoEstado.EnCurso || nuevoEstado == RetoEstado.Cancelado;
                break;
            case RetoEstado.EnCurso:
                esValido = nuevoEstado == RetoEstado.Cerrado || nuevoEstado == RetoEstado.Cancelado;
                break;
            case RetoEstado.Cerrado:
            case RetoEstado.Cancelado:
                esValido = false;
                break;
        }

        if (!esValido)
            throw new ConflictException($"Transición no permitida de '{reto.Estado}' a '{nuevoEstado}'.");

        reto.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
    }

    public async Task EliminarAsync(Guid empresaId, Guid retoId, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        if (reto.Estado != RetoEstado.Borrador)
            throw new ConflictException("Solo se pueden eliminar retos en estado Borrador.");

        _context.Remove(reto);
        await _context.SaveChangesAsync(ct);
    }
}

