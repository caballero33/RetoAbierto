using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Seguimiento.DTOs
{
    public class CrearMetaRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime FechaLimite { get; set; }
    }

    public class MetaResponse
    {
        public Guid Id { get; set; }
        public Guid AsignacionId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string FechaLimite { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class CambiarEstadoMetaRequest
    {
        public string Estado { get; set; } = string.Empty;
    }

    public class RegistrarAvanceRequest
    {
        public string Descripcion { get; set; } = string.Empty;
        public string? EvidenciaUrl { get; set; }
    }

    public class AvanceResponse
    {
        public Guid Id { get; set; }
        public Guid MetaId { get; set; }
        public Guid EstudianteId { get; set; }
        public string EstudianteNombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? EvidenciaUrl { get; set; }
        public string FechaRegistro { get; set; } = string.Empty;
    }

    public class RegistrarHorasRequest
    {
        public DateTime Fecha { get; set; }
        public int Horas { get; set; }
        public string? Descripcion { get; set; }
    }

    public class RegistroHorasResponse
    {
        public Guid Id { get; set; }
        public Guid EstudianteId { get; set; }
        public string EstudianteNombre { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public int Horas { get; set; }
        public string? Descripcion { get; set; }
    }
}

namespace RetoAbierto.Application.Features.Seguimiento.Validators
{
    using RetoAbierto.Application.Features.Seguimiento.DTOs;

    public class CrearMetaValidator : AbstractValidator<CrearMetaRequest>
    {
        public CrearMetaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(150).WithMessage("El título no debe superar los 150 caracteres.");
        }
    }

    public class RegistrarAvanceValidator : AbstractValidator<RegistrarAvanceRequest>
    {
        public RegistrarAvanceValidator()
        {
            RuleFor(x => x.Descripcion)
                .NotEmpty().WithMessage("La descripción del avance es obligatoria.");

            RuleFor(x => x.EvidenciaUrl)
                .Must(uri => string.IsNullOrEmpty(uri) || Uri.TryCreate(uri, UriKind.Absolute, out _))
                .WithMessage("La URL de evidencia no tiene un formato válido.");
        }
    }

    public class RegistrarHorasValidator : AbstractValidator<RegistrarHorasRequest>
    {
        public RegistrarHorasValidator()
        {
            RuleFor(x => x.Horas)
                .InclusiveBetween(1, 24).WithMessage("Las horas deben ser mayor que cero y menor o igual a 24.");
        }
    }
}

namespace RetoAbierto.Application.Features.Seguimiento.Services
{
    using RetoAbierto.Application.Features.Seguimiento.DTOs;

    public interface ISeguimientoService
    {
        Task<List<MetaResponse>> ListarMetasAsync(Guid asignacionId, CancellationToken ct = default);
        Task<MetaResponse> CrearMetaAsync(Guid empresaId, Guid asignacionId, CrearMetaRequest request, CancellationToken ct = default);
        Task CambiarEstadoMetaAsync(Guid empresaId, Guid metaId, string nuevoEstadoStr, CancellationToken ct = default);

        Task<List<AvanceResponse>> ListarAvancesAsync(Guid metaId, CancellationToken ct = default);
        Task<AvanceResponse> RegistrarAvanceAsync(Guid estudianteId, Guid metaId, RegistrarAvanceRequest request, CancellationToken ct = default);

        Task<List<RegistroHorasResponse>> ListarHorasAsync(Guid asignacionId, CancellationToken ct = default);
        Task<RegistroHorasResponse> RegistrarHorasAsync(Guid estudianteId, Guid asignacionId, RegistrarHorasRequest request, CancellationToken ct = default);
    }

    public class SeguimientoService : ISeguimientoService
    {
        private readonly IApplicationDbContext _context;

        public SeguimientoService(IApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<MetaResponse>> ListarMetasAsync(Guid asignacionId, CancellationToken ct = default)
        {
            var metas = _context.Metas
                .Where(m => m.AsignacionId == asignacionId)
                .Select(m => new MetaResponse
                {
                    Id = m.Id,
                    AsignacionId = m.AsignacionId,
                    Titulo = m.Titulo,
                    Descripcion = m.Descripcion,
                    FechaLimite = m.FechaLimite.ToString("yyyy-MM-dd"),
                    Estado = m.Estado.ToString()
                }).ToList();

            return Task.FromResult(metas);
        }

        public async Task<MetaResponse> CrearMetaAsync(Guid empresaId, Guid asignacionId, CrearMetaRequest request, CancellationToken ct = default)
        {
            var asignacion = _context.Asignaciones.FirstOrDefault(a => a.Id == asignacionId && a.EmpresaId == empresaId);
            if (asignacion == null)
                throw new NotFoundException("Asignación no encontrada.");

            var meta = new Meta
            {
                EmpresaId = empresaId,
                AsignacionId = asignacionId,
                Titulo = request.Titulo,
                Descripcion = request.Descripcion,
                FechaLimite = request.FechaLimite,
                Estado = MetaEstado.Pendiente
            };
            await _context.AddAsync(meta, ct);
            await _context.SaveChangesAsync(ct);

            return new MetaResponse
            {
                Id = meta.Id,
                AsignacionId = meta.AsignacionId,
                Titulo = meta.Titulo,
                Descripcion = meta.Descripcion,
                FechaLimite = meta.FechaLimite.ToString("yyyy-MM-dd"),
                Estado = meta.Estado.ToString()
            };
        }

        public async Task CambiarEstadoMetaAsync(Guid empresaId, Guid metaId, string nuevoEstadoStr, CancellationToken ct = default)
        {
            if (!Enum.TryParse<MetaEstado>(nuevoEstadoStr, true, out var nuevoEstado))
                throw new AppValidationException("estado", "Estado de meta inválido. Valores: Pendiente, EnProgreso, Cumplida.");

            var meta = _context.Metas.FirstOrDefault(m => m.Id == metaId && m.EmpresaId == empresaId);
            if (meta == null)
                throw new NotFoundException("Meta no encontrada.");

            meta.Estado = nuevoEstado;
            await _context.SaveChangesAsync(ct);
        }

        public Task<List<AvanceResponse>> ListarAvancesAsync(Guid metaId, CancellationToken ct = default)
        {
            var avances = _context.Avances
                .Where(a => a.MetaId == metaId)
                .Select(a => new AvanceResponse
                {
                    Id = a.Id,
                    MetaId = a.MetaId,
                    EstudianteId = a.EstudianteId,
                    EstudianteNombre = a.Estudiante.Usuario.Nombre,
                    Descripcion = a.Descripcion,
                    EvidenciaUrl = a.EvidenciaUrl,
                    FechaRegistro = a.FechaRegistro.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToList();

            return Task.FromResult(avances);
        }

        public async Task<AvanceResponse> RegistrarAvanceAsync(Guid estudianteId, Guid metaId, RegistrarAvanceRequest request, CancellationToken ct = default)
        {
            var meta = _context.Metas.FirstOrDefault(m => m.Id == metaId);
            if (meta == null)
                throw new NotFoundException("Meta no encontrada.");

            var esParteDelEquipo = _context.Asignaciones.Any(a =>
                a.Id == meta.AsignacionId &&
                a.Postulacion.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId));

            if (!esParteDelEquipo)
                throw new ForbiddenException("El estudiante no pertenece al equipo asignado a esta meta.");

            // CORRECCIÓN DE COMPATIBILIDAD: antes se leía estudiante.Usuario.Nombre sin cargar Usuario.
            // La prueba de avances detectó una referencia nula. Ahora se consulta directamente el nombre,
            // conservando la comprobación anterior de pertenencia a la asignación.
            var nombreEstudiante = _context.Estudiantes.Where(e => e.Id == estudianteId).Select(e => e.Usuario.Nombre).FirstOrDefault();

            var avance = new Avance
            {
                EmpresaId = meta.EmpresaId,
                MetaId = metaId,
                EstudianteId = estudianteId,
                Descripcion = request.Descripcion,
                EvidenciaUrl = request.EvidenciaUrl,
                FechaRegistro = DateTime.UtcNow
            };
            await _context.AddAsync(avance, ct);
            await _context.SaveChangesAsync(ct);

            return new AvanceResponse
            {
                Id = avance.Id,
                MetaId = avance.MetaId,
                EstudianteId = estudianteId,
                EstudianteNombre = nombreEstudiante ?? "Estudiante",
                Descripcion = avance.Descripcion,
                EvidenciaUrl = avance.EvidenciaUrl,
                FechaRegistro = avance.FechaRegistro.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }

        public Task<List<RegistroHorasResponse>> ListarHorasAsync(Guid asignacionId, CancellationToken ct = default)
        {
            var horas = _context.RegistroHoras
                .Where(r => r.AsignacionId == asignacionId)
                .Select(r => new RegistroHorasResponse
                {
                    Id = r.Id,
                    EstudianteId = r.EstudianteId,
                    EstudianteNombre = r.Estudiante.Usuario.Nombre,
                    Fecha = r.Fecha.ToString("yyyy-MM-dd"),
                    Horas = r.Horas,
                    Descripcion = r.Descripcion
                }).ToList();

            return Task.FromResult(horas);
        }

        public async Task<RegistroHorasResponse> RegistrarHorasAsync(Guid estudianteId, Guid asignacionId, RegistrarHorasRequest request, CancellationToken ct = default)
        {
            var asignacion = _context.Asignaciones.FirstOrDefault(a => a.Id == asignacionId);
            if (asignacion == null)
                throw new NotFoundException("Asignación no encontrada.");

            var esParteDelEquipo = asignacion.Postulacion.Equipo.Integrantes.Any(i => i.EstudianteId == estudianteId);
            if (!esParteDelEquipo)
                throw new ForbiddenException("El estudiante no forma parte del equipo de esta asignación.");

            if (request.Fecha < asignacion.FechaInicio.Date || request.Fecha > asignacion.FechaFin.Date)
                throw new ConflictException("La fecha debe encontrarse dentro del rango de la asignación.");

            // CORRECCIÓN DE COMPATIBILIDAD: el registro de horas tenía el mismo acceso a Usuario no cargado.
            // Se obtiene el nombre en la consulta para construir la respuesta, sin cambiar los permisos
            // de participación ni la validación de fechas que se comprueban arriba.
            var nombreEstudiante = _context.Estudiantes.Where(e => e.Id == estudianteId).Select(e => e.Usuario.Nombre).FirstOrDefault();

            var reg = new RegistroHoras
            {
                EmpresaId = asignacion.EmpresaId,
                AsignacionId = asignacionId,
                EstudianteId = estudianteId,
                Fecha = request.Fecha,
                Horas = request.Horas,
                Descripcion = request.Descripcion
            };
            await _context.AddAsync(reg, ct);
            await _context.SaveChangesAsync(ct);

            return new RegistroHorasResponse
            {
                Id = reg.Id,
                EstudianteId = estudianteId,
                EstudianteNombre = nombreEstudiante ?? "Estudiante",
                Fecha = reg.Fecha.ToString("yyyy-MM-dd"),
                Horas = reg.Horas,
                Descripcion = reg.Descripcion
            };
        }
    }
}

