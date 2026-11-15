using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Domain.Entities;

public class Meta : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid AsignacionId { get; set; }
    public virtual Asignacion Asignacion { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaLimite { get; set; }
    public MetaEstado Estado { get; set; } = MetaEstado.Pendiente;

    public virtual ICollection<Avance> Avances { get; set; } = new List<Avance>();
}

public class Avance : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid MetaId { get; set; }
    public virtual Meta Meta { get; set; } = null!;

    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public string Descripcion { get; set; } = string.Empty;
    public string? EvidenciaUrl { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
}

public class RegistroHoras : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid AsignacionId { get; set; }
    public virtual Asignacion Asignacion { get; set; } = null!;

    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public DateTime Fecha { get; set; }
    public int Horas { get; set; }
    public string? Descripcion { get; set; }
}

public class Evaluacion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid AsignacionId { get; set; }
    public virtual Asignacion Asignacion { get; set; } = null!;

    public Guid EvaluadoPorUsuarioId { get; set; }
    public virtual Usuario EvaluadoPorUsuario { get; set; } = null!;

    public int Calificacion { get; set; }
    public string? Comentarios { get; set; }
    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
}

