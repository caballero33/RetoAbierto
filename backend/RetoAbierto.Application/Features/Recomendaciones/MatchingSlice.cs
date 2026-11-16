using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;

namespace RetoAbierto.Application.Features.Recomendaciones.DTOs
{
    public class DetalleHabilidadMatchingDto
    {
        public string Habilidad { get; set; } = string.Empty;
        public int NivelRequerido { get; set; }
        public int NivelEstudiante { get; set; }
        public double Cumplimiento { get; set; }
    }

    public class DesgloseHabilidadesDto
    {
        public double Puntaje { get; set; }
        public double Peso { get; set; } = 0.5;
        public List<DetalleHabilidadMatchingDto> Detalle { get; set; } = new();
    }

    public class DesgloseDisponibilidadDto
    {
        public double Puntaje { get; set; }
        public double Peso { get; set; } = 0.3;
        public int HorasDisponibles { get; set; }
        public int HorasRequeridas { get; set; }
    }

    public class DesgloseExperienciaDto
    {
        public double Puntaje { get; set; }
        public double Peso { get; set; } = 0.2;
        public List<string> Coincidencias { get; set; } = new();
    }

    public class DesgloseMatchingDto
    {
        public DesgloseHabilidadesDto Habilidades { get; set; } = new();
        public DesgloseDisponibilidadDto Disponibilidad { get; set; } = new();
        public DesgloseExperienciaDto Experiencia { get; set; } = new();
    }

    public class RecomendacionResponse
    {
        public Guid EstudianteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Carrera { get; set; } = string.Empty;
        public double PuntajeTotal { get; set; }
        public DesgloseMatchingDto Desglose { get; set; } = new();
    }
}

namespace RetoAbierto.Application.Features.Recomendaciones.Services
{
    using RetoAbierto.Application.Features.Recomendaciones.DTOs;

    public interface IMatchingService
    {
        Task<List<RecomendacionResponse>> ObtenerRecomendacionesAsync(Guid empresaId, Guid retoId, int top = 10, CancellationToken ct = default);
    }

    public class MatchingService : IMatchingService
    {
        private readonly IApplicationDbContext _context;

        public MatchingService(IApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<RecomendacionResponse>> ObtenerRecomendacionesAsync(Guid empresaId, Guid retoId, int top = 10, CancellationToken ct = default)
        {
            if (top < 1) top = 1;
            if (top > 50) top = 50;

            var reto = _context.Retos.FirstOrDefault(r => r.Id == retoId && r.EmpresaId == empresaId);
            if (reto == null)
                throw new NotFoundException("Reto no encontrado.");

            var habilidadesRequeridas = _context.RetoHabilidades
                .Where(rh => rh.RetoId == retoId)
                .Select(rh => new { rh.HabilidadId, rh.Habilidad.Nombre, rh.NivelRequerido })
                .ToList();

            if (!habilidadesRequeridas.Any())
                return Task.FromResult(new List<RecomendacionResponse>());

            var estudiantesActivos = _context.Estudiantes
                .Where(e => e.Activo)
                .Select(e => new
                {
                    e.Id,
                    e.Usuario.Nombre,
                    e.Carrera,
                    e.DisponibilidadHoras,
                    e.Experiencia,
                    Habilidades = e.Habilidades.Select(h => new { h.HabilidadId, h.Nivel }).ToList()
                }).ToList();

            double diasRestantes = Math.Max((reto.FechaLimite - DateTime.UtcNow).TotalDays, 7);
            double semanas = Math.Max(diasRestantes / 7.0, 1.0);
            int horasRequeridasSemanales = Math.Max((int)Math.Ceiling(reto.HorasEstimadas / semanas), 1);

            var resultados = new List<RecomendacionResponse>();

            foreach (var est in estudiantesActivos)
            {

                var detallesHabilidad = new List<DetalleHabilidadMatchingDto>();
                double sumaCumplimiento = 0;

                foreach (var req in habilidadesRequeridas)
                {
                    var tiene = est.Habilidades.FirstOrDefault(h => h.HabilidadId == req.HabilidadId);
                    int nivelEst = tiene?.Nivel ?? 0;
                    double cumplimiento = Math.Min((double)nivelEst / req.NivelRequerido, 1.0);
                    sumaCumplimiento += cumplimiento;

                    detallesHabilidad.Add(new DetalleHabilidadMatchingDto
                    {
                        Habilidad = req.Nombre,
                        NivelRequerido = req.NivelRequerido,
                        NivelEstudiante = nivelEst,
                        Cumplimiento = Math.Round(cumplimiento, 2)
                    });
                }

                double puntajeH = Math.Round(sumaCumplimiento / habilidadesRequeridas.Count, 2);

                double puntajeD = Math.Round(Math.Min((double)est.DisponibilidadHoras / horasRequeridasSemanales, 1.0), 2);

                var coincidencias = new List<string>();
                string textoExp = (est.Experiencia ?? string.Empty).ToLower();

                foreach (var req in habilidadesRequeridas)
                {
                    if (textoExp.Contains(req.Nombre.ToLower()))
                    {
                        coincidencias.Add(req.Nombre);
                    }
                }

                double puntajeE = Math.Round((double)coincidencias.Count / habilidadesRequeridas.Count, 2);

                double puntajeTotal = Math.Round(100 * ((0.5 * puntajeH) + (0.3 * puntajeD) + (0.2 * puntajeE)), 1);

                resultados.Add(new RecomendacionResponse
                {
                    EstudianteId = est.Id,
                    Nombre = est.Nombre,
                    Carrera = est.Carrera,
                    PuntajeTotal = puntajeTotal,
                    Desglose = new DesgloseMatchingDto
                    {
                        Habilidades = new DesgloseHabilidadesDto
                        {
                            Puntaje = puntajeH,
                            Peso = 0.5,
                            Detalle = detallesHabilidad
                        },
                        Disponibilidad = new DesgloseDisponibilidadDto
                        {
                            Puntaje = puntajeD,
                            Peso = 0.3,
                            HorasDisponibles = est.DisponibilidadHoras,
                            HorasRequeridas = horasRequeridasSemanales
                        },
                        Experiencia = new DesgloseExperienciaDto
                        {
                            Puntaje = puntajeE,
                            Peso = 0.2,
                            Coincidencias = coincidencias
                        }
                    }
                });
            }

            var topResultados = resultados
                .OrderByDescending(r => r.PuntajeTotal)
                .Take(top)
                .ToList();

            return Task.FromResult(topResultados);
        }
    }
}

