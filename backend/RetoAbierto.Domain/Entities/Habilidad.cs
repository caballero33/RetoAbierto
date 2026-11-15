using RetoAbierto.Domain.Common;

namespace RetoAbierto.Domain.Entities;

public class Habilidad : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;

    public virtual ICollection<EstudianteHabilidad> Estudiantes { get; set; } = new List<EstudianteHabilidad>();
    public virtual ICollection<RetoHabilidad> Retos { get; set; } = new List<RetoHabilidad>();
}

public class EstudianteHabilidad
{
    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public Guid HabilidadId { get; set; }
    public virtual Habilidad Habilidad { get; set; } = null!;

    public int Nivel { get; set; }
}

public class RetoHabilidad : ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RetoId { get; set; }
    public virtual Reto Reto { get; set; } = null!;

    public Guid HabilidadId { get; set; }
    public virtual Habilidad Habilidad { get; set; } = null!;

    public int NivelRequerido { get; set; }
}

