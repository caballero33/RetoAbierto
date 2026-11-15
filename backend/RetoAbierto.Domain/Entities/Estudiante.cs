using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Domain.Entities;

public class Estudiante : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    public Guid UniversidadId { get; set; }
    public virtual Universidad Universidad { get; set; } = null!;

    public Guid RegistradoPorUsuarioId { get; set; }
    public virtual Usuario RegistradoPorUsuario { get; set; } = null!;

    public string Carrera { get; set; } = string.Empty;
    public int DisponibilidadHoras { get; set; }
    public string? Experiencia { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? FechaDesactivacion { get; set; }

    public virtual ICollection<Beca> Becas { get; set; } = new List<Beca>();
    public virtual ICollection<EstudianteHabilidad> Habilidades { get; set; } = new List<EstudianteHabilidad>();
    public virtual ICollection<EquipoIntegrante> Equipos { get; set; } = new List<EquipoIntegrante>();
    public virtual ICollection<Avance> Avances { get; set; } = new List<Avance>();
    public virtual ICollection<RegistroHoras> RegistrosHoras { get; set; } = new List<RegistroHoras>();
    public virtual ICollection<RecompensaEntrega> RecompensasEntregadas { get; set; } = new List<RecompensaEntrega>();
}

public class Beca : BaseEntity
{
    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public Guid? ValidadaPorUsuarioId { get; set; }
    public virtual Usuario? ValidadaPorUsuario { get; set; }

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public BecaEstado Estado { get; set; } = BecaEstado.Pendiente;
    public DateTime? FechaValidacion { get; set; }
}

