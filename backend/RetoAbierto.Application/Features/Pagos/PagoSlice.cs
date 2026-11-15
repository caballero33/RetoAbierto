using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Pagos.DTOs;

public class CrearPagoRequest
{
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
}

public class PagoResponse
{
    public Guid Id { get; set; }
    public Guid RetoId { get; set; }
    public decimal Monto { get; set; }
    public string FechaPago { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Referencia { get; set; }
}

public class CambiarEstadoPagoRequest
{
    public string Estado { get; set; } = string.Empty;
}

namespace RetoAbierto.Application.Features.Pagos.Validators;

using RetoAbierto.Application.Features.Pagos.DTOs;

public class CrearPagoValidator : AbstractValidator<CrearPagoRequest>
{
    public CrearPagoValidator()
    {
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor que cero.")
            .ScalePrecision(2, 18).WithMessage("El monto permite hasta dos decimales.");

        RuleFor(x => x.Referencia)
            .MaximumLength(150).WithMessage("La referencia no debe superar los 150 caracteres.");
    }
}

public class CambiarEstadoPagoValidator : AbstractValidator<CambiarEstadoPagoRequest>
{
    public CambiarEstadoPagoValidator()
    {
        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .Must(e => Enum.TryParse<PagoEstado>(e, true, out _))
            .WithMessage("Los estados permitidos son Pendiente, Pagado y Anulado.");
    }
}

namespace RetoAbierto.Application.Features.Pagos.Services;

using RetoAbierto.Application.Features.Pagos.DTOs;

public interface IPagoService
{
    Task<List<PagoResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, CancellationToken ct = default);
    Task<PagoResponse> CrearAsync(Guid empresaId, Guid retoId, CrearPagoRequest request, CancellationToken ct = default);
    Task CambiarEstadoAsync(Guid empresaId, Guid pagoId, string nuevoEstadoStr, CancellationToken ct = default);
}

public class PagoService : IPagoService
{
    private readonly IApplicationDbContext _context;

    public PagoService(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<PagoResponse>> ListarPorRetoAsync(Guid empresaId, Guid retoId, CancellationToken ct = default)
    {
        var pagos = _context.Pagos
            .Where(p => p.EmpresaId == empresaId && p.RetoId == retoId)
            .Select(p => new PagoResponse
            {
                Id = p.Id,
                RetoId = p.RetoId,
                Monto = p.Monto,
                FechaPago = p.FechaPago.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Estado = p.Estado.ToString(),
                Referencia = p.Referencia
            }).ToList();

        return Task.FromResult(pagos);
    }

    public async Task<PagoResponse> CrearAsync(Guid empresaId, Guid retoId, CrearPagoRequest request, CancellationToken ct = default)
    {
        var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
        if (reto == null)
            throw new NotFoundException("Reto no encontrado.");

        var pago = new Pago
        {
            EmpresaId = empresaId,
            RetoId = retoId,
            Monto = request.Monto,
            FechaPago = DateTime.UtcNow,
            Estado = PagoEstado.Pendiente,
            Referencia = request.Referencia
        };
        await _context.AddAsync(pago, ct);
        await _context.SaveChangesAsync(ct);

        return new PagoResponse
        {
            Id = pago.Id,
            RetoId = pago.RetoId,
            Monto = pago.Monto,
            FechaPago = pago.FechaPago.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Estado = pago.Estado.ToString(),
            Referencia = pago.Referencia
        };
    }

    public async Task CambiarEstadoAsync(Guid empresaId, Guid pagoId, string nuevoEstadoStr, CancellationToken ct = default)
    {
        if (!Enum.TryParse<PagoEstado>(nuevoEstadoStr, true, out var nuevoEstado))
            throw new AppValidationException("estado", "Estado de pago inválido.");

        var pago = _context.Pagos.FirstOrDefault(p => p.Id == pagoId && p.EmpresaId == empresaId);
        if (pago == null)
            throw new NotFoundException("Pago no encontrado.");

        pago.Estado = nuevoEstado;
        await _context.SaveChangesAsync(ct);
    }
}

