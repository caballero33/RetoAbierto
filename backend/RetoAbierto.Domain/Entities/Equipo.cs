using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Domain.Entities;

public class Equipo : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RetoId { get; set; }
    public virtual Reto Reto { get; set; } = null!;

    public string Nombre { get; set; } = string.Empty;
    public EquipoEstado Estado { get; set; } = EquipoEstado.Activo;

    public virtual ICollection<EquipoIntegrante> Integrantes { get; set; } = new List<EquipoIntegrante>();
    public virtual ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
}

public class EquipoIntegrante : ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid EquipoId { get; set; }
    public virtual Equipo Equipo { get; set; } = null!;

    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public bool EsLider { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}

public class Postulacion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RetoId { get; set; }
    public virtual Reto Reto { get; set; } = null!;

    public Guid EquipoId { get; set; }
    public virtual Equipo Equipo { get; set; } = null!;

    public string Propuesta { get; set; } = string.Empty;
    public PostulacionEstado Estado { get; set; } = PostulacionEstado.Enviada;
    public DateTime FechaPostulacion { get; set; } = DateTime.UtcNow;

    public virtual Asignacion? Asignacion { get; set; }
}

public class Asignacion : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid PostulacionId { get; set; }
    public virtual Postulacion Postulacion { get; set; } = null!;

    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public AsignacionEstado Estado { get; set; } = AsignacionEstado.EnCurso;

    public virtual ICollection<Meta> Metas { get; set; } = new List<Meta>();
    public virtual ICollection<RegistroHoras> RegistrosHoras { get; set; } = new List<RegistroHoras>();
    public virtual ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
}

