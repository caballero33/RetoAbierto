using RetoAbierto.Domain.Common;

namespace RetoAbierto.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EsAdminPlataforma { get; set; } = false;
    public bool DebeCambiarPassword { get; set; } = false;
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public virtual ICollection<EmpresaUsuario> EmpresaUsuarios { get; set; } = new List<EmpresaUsuario>();
    public virtual ICollection<UniversidadUsuario> UniversidadUsuarios { get; set; } = new List<UniversidadUsuario>();
    public virtual Estudiante? Estudiante { get; set; }
}

