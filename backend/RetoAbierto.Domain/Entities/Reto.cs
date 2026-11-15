using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Domain.Entities;

public class Reto : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Entregables { get; set; } = string.Empty;
    public int MaxIntegrantes { get; set; }
    public int HorasEstimadas { get; set; }
    public DateTime FechaLimite { get; set; }
    public RetoEstado Estado { get; set; } = RetoEstado.Borrador;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public virtual ICollection<RetoHabilidad> Habilidades { get; set; } = new List<RetoHabilidad>();
    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    public virtual ICollection<Recompensa> Recompensas { get; set; } = new List<Recompensa>();
    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
    public virtual ICollection<Postulacion> Postulaciones { get; set; } = new List<Postulacion>();
}

public class Pago : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RetoId { get; set; }
    public virtual Reto Reto { get; set; } = null!;

    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public PagoEstado Estado { get; set; } = PagoEstado.Pendiente;
    public string? Referencia { get; set; }
}

public class Recompensa : BaseEntity, ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RetoId { get; set; }
    public virtual Reto Reto { get; set; } = null!;

    public string Descripcion { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public decimal Valor { get; set; }

    public virtual ICollection<RecompensaEntrega> Entregas { get; set; } = new List<RecompensaEntrega>();
}

public class RecompensaEntrega : ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid RecompensaId { get; set; }
    public virtual Recompensa Recompensa { get; set; } = null!;

    public Guid EstudianteId { get; set; }
    public virtual Estudiante Estudiante { get; set; } = null!;

    public DateTime FechaEntrega { get; set; } = DateTime.UtcNow;
    public RecompensaEntregaEstado Estado { get; set; } = RecompensaEntregaEstado.Pendiente;
}

