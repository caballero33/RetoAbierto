using RetoAbierto.Domain.Common;

namespace RetoAbierto.Domain.Entities;

public class Universidad : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    public virtual ICollection<UniversidadUsuario> UniversidadUsuarios { get; set; } = new List<UniversidadUsuario>();
    public virtual ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}

public class UniversidadUsuario
{
    public Guid UniversidadId { get; set; }
    public virtual Universidad Universidad { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;

    public bool Activo { get; set; } = true;
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
}

