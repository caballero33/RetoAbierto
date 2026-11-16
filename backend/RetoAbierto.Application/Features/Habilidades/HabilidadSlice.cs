using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Estudiantes.DTOs;
using RetoAbierto.Application.Features.Retos.DTOs;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Habilidades.DTOs
{
    public class HabilidadResponse
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class CrearHabilidadRequest
    {
        public string Nombre { get; set; } = string.Empty;
    }

    public class ItemHabilidadEstudianteDto
    {
        public Guid HabilidadId { get; set; }
        public int Nivel { get; set; }
    }

    public class ActualizarHabilidadesEstudianteRequest
    {
        public List<ItemHabilidadEstudianteDto> Habilidades { get; set; } = new();
    }

    public class ActualizarHabilidadesRetoRequest
    {
        public List<HabilidadRequeridaDto> Habilidades { get; set; } = new();
    }
}

namespace RetoAbierto.Application.Features.Habilidades.Validators
{
    using RetoAbierto.Application.Features.Habilidades.DTOs;

    public class CrearHabilidadValidator : AbstractValidator<CrearHabilidadRequest>
    {
        public CrearHabilidadValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre de la habilidad es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no debe superar los 100 caracteres.");
        }
    }

    public class ActualizarHabilidadesEstudianteValidator : AbstractValidator<ActualizarHabilidadesEstudianteRequest>
    {
        public ActualizarHabilidadesEstudianteValidator()
        {
            RuleFor(x => x.Habilidades)
                .NotNull()
                .Must(h => h.Select(x => x.HabilidadId).Distinct().Count() == h.Count)
                .WithMessage("No se permiten habilidades repetidas.");

            RuleForEach(x => x.Habilidades).ChildRules(h =>
            {
                h.RuleFor(x => x.Nivel)
                    .InclusiveBetween(1, 5).WithMessage("El nivel debe encontrarse entre 1 y 5.");
            });
        }
    }

    public class ActualizarHabilidadesRetoValidator : AbstractValidator<ActualizarHabilidadesRetoRequest>
    {
        public ActualizarHabilidadesRetoValidator()
        {
            RuleFor(x => x.Habilidades)
                .NotEmpty().WithMessage("El reto debe contener al menos una habilidad requerida.")
                .Must(h => h.Select(x => x.HabilidadId).Distinct().Count() == h.Count)
                .WithMessage("No se permiten habilidades repetidas.");

            RuleForEach(x => x.Habilidades).ChildRules(h =>
            {
                h.RuleFor(x => x.NivelRequerido)
                    .InclusiveBetween(1, 5).WithMessage("El nivel requerido debe encontrarse entre 1 y 5.");
            });
        }
    }
}

namespace RetoAbierto.Application.Features.Habilidades.Services
{
    using RetoAbierto.Application.Features.Habilidades.DTOs;

    public interface IHabilidadService
    {
        Task<List<HabilidadResponse>> ListarAsync(string? q, CancellationToken ct = default);
        Task<HabilidadResponse> CrearAsync(CrearHabilidadRequest request, CancellationToken ct = default);
        Task<PerfilEstudianteResponse> ActualizarHabilidadesEstudianteAsync(Guid usuarioId, ActualizarHabilidadesEstudianteRequest request, CancellationToken ct = default);
        Task<RetoResponse> ActualizarHabilidadesRetoAsync(Guid empresaId, Guid retoId, ActualizarHabilidadesRetoRequest request, CancellationToken ct = default);
    }

    public class HabilidadService : IHabilidadService
    {
        private readonly IApplicationDbContext _context;

        public HabilidadService(IApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<HabilidadResponse>> ListarAsync(string? q, CancellationToken ct = default)
        {
            var query = _context.Habilidades.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(h => h.Nombre.ToLower().Contains(q.ToLower()));

            var list = query
                .OrderBy(h => h.Nombre)
                .Select(h => new HabilidadResponse
                {
                    Id = h.Id,
                    Nombre = h.Nombre
                }).ToList();

            return Task.FromResult(list);
        }

        public async Task<HabilidadResponse> CrearAsync(CrearHabilidadRequest request, CancellationToken ct = default)
        {
            var existe = _context.Habilidades.Any(h => h.Nombre.ToLower() == request.Nombre.ToLower());
            if (existe)
                throw new ConflictException("Ya existe una habilidad con este nombre.");

            var hab = new Habilidad { Nombre = request.Nombre };
            await _context.AddAsync(hab, ct);
            await _context.SaveChangesAsync(ct);

            return new HabilidadResponse { Id = hab.Id, Nombre = hab.Nombre };
        }

        public async Task<PerfilEstudianteResponse> ActualizarHabilidadesEstudianteAsync(Guid usuarioId, ActualizarHabilidadesEstudianteRequest request, CancellationToken ct = default)
        {
            var estudiante = _context.Estudiantes.FirstOrDefault(e => e.UsuarioId == usuarioId);
            if (estudiante == null)
                throw new NotFoundException("Estudiante no encontrado.");

            var anteriores = _context.EstudianteHabilidades.Where(eh => eh.EstudianteId == estudiante.Id).ToList();
            foreach (var ant in anteriores)
            {
                _context.Remove(ant);
            }

            foreach (var item in request.Habilidades)
            {
                var h = _context.Habilidades.FirstOrDefault(x => x.Id == item.HabilidadId);
                if (h == null)
                    throw new NotFoundException($"Habilidad {item.HabilidadId} no encontrada en catálogo.");

                var eh = new EstudianteHabilidad
                {
                    EstudianteId = estudiante.Id,
                    HabilidadId = item.HabilidadId,
                    Nivel = item.Nivel
                };
                await _context.AddAsync(eh, ct);
            }

            await _context.SaveChangesAsync(ct);

            var habilidadesDto = _context.EstudianteHabilidades
                .Where(eh => eh.EstudianteId == estudiante.Id)
                .Select(eh => new HabilidadEstudianteItemDto
                {
                    HabilidadId = eh.HabilidadId,
                    Nombre = eh.Habilidad.Nombre,
                    Nivel = eh.Nivel
                }).ToList();

            return new PerfilEstudianteResponse
            {
                Id = estudiante.Id,
                Nombre = estudiante.Usuario.Nombre,
                Correo = estudiante.Usuario.Correo,
                Universidad = estudiante.Universidad.Nombre,
                Carrera = estudiante.Carrera,
                DisponibilidadHoras = estudiante.DisponibilidadHoras,
                Experiencia = estudiante.Experiencia,
                Activo = estudiante.Activo,
                Habilidades = habilidadesDto
            };
        }

        public async Task<RetoResponse> ActualizarHabilidadesRetoAsync(Guid empresaId, Guid retoId, ActualizarHabilidadesRetoRequest request, CancellationToken ct = default)
        {
            var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
            if (reto == null)
                throw new NotFoundException("Reto no encontrado.");

            var anteriores = _context.RetoHabilidades.Where(rh => rh.RetoId == retoId).ToList();
            foreach (var ant in anteriores)
            {
                _context.Remove(ant);
            }

            foreach (var item in request.Habilidades)
            {
                var h = _context.Habilidades.FirstOrDefault(x => x.Id == item.HabilidadId);
                if (h == null)
                    throw new NotFoundException($"Habilidad {item.HabilidadId} no encontrada.");

                var rh = new RetoHabilidad
                {
                    EmpresaId = empresaId,
                    RetoId = retoId,
                    HabilidadId = item.HabilidadId,
                    NivelRequerido = item.NivelRequerido
                };
                await _context.AddAsync(rh, ct);
            }

            await _context.SaveChangesAsync(ct);

            var lista = _context.RetoHabilidades
                .Where(rh => rh.RetoId == retoId)
                .Select(rh => new HabilidadRequeridaDetalleDto
                {
                    HabilidadId = rh.HabilidadId,
                    Nombre = rh.Habilidad.Nombre,
                    NivelRequerido = rh.NivelRequerido
                }).ToList();

            return new RetoResponse
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
                Habilidades = lista
            };
        }
    }
}

