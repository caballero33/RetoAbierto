using RetoAbierto.Domain.Common;

namespace RetoAbierto.Domain.Entities;

public class Empresa : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Correo { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual ICollection<EmpresaUsuario> EmpresaUsuarios { get; set; } = new List<EmpresaUsuario>();
    public virtual ICollection<Reto> Retos { get; set; } = new List<Reto>();
}

public class Rol : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public virtual ICollection<EmpresaUsuario> EmpresaUsuarios { get; set; } = new List<EmpresaUsuario>();
}

public class EmpresaUsuario : ITenantEntity
{
    public Guid EmpresaId { get; set; }
    public virtual Empresa Empresa { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    public Guid RolId { get; set; }
    public virtual Rol Rol { get; set; } = null!;

    public bool Activo { get; set; } = true;
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}

