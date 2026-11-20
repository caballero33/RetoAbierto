using RetoAbierto.Domain.Entities;

namespace RetoAbierto.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Usuario> Usuarios { get; }
    IQueryable<Universidad> Universidades { get; }
    IQueryable<UniversidadUsuario> UniversidadUsuarios { get; }
    IQueryable<Estudiante> Estudiantes { get; }
    IQueryable<Beca> Becas { get; }
    IQueryable<Empresa> Empresas { get; }
    IQueryable<Rol> Roles { get; }
    IQueryable<EmpresaUsuario> EmpresaUsuarios { get; }
    IQueryable<Reto> Retos { get; }
    IQueryable<Pago> Pagos { get; }
    IQueryable<Recompensa> Recompensas { get; }
    IQueryable<RecompensaEntrega> RecompensaEntregas { get; }
    IQueryable<Equipo> Equipos { get; }
    IQueryable<EquipoIntegrante> EquipoIntegrantes { get; }
    IQueryable<Postulacion> Postulaciones { get; }
    IQueryable<Asignacion> Asignaciones { get; }
    IQueryable<Meta> Metas { get; }
    IQueryable<Avance> Avances { get; }
    IQueryable<RegistroHoras> RegistroHoras { get; }
    IQueryable<Evaluacion> Evaluaciones { get; }
    IQueryable<Habilidad> Habilidades { get; }
    IQueryable<EstudianteHabilidad> EstudianteHabilidades { get; }
    IQueryable<RetoHabilidad> RetoHabilidades { get; }

    // consultar membresias despues de verificar la identidad.
    // Correcion de conpatibilidad: cerrar el filtro empresarial ocultaría las membresias
    // durante el login o al cambiar de empresa esta consulta exige indicar el usuario verificado
    // y solo devuelve sus vínculos activos, no concede acceso a las membresias de todos.
    IQueryable<EmpresaUsuario> MembresiasDeUsuario(Guid usuarioId);

    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ITenantService
{
    Guid? EmpresaId { get; }
    Guid? UsuarioId { get; }
    string? Rol { get; }
    Guid? UniversidadId { get; }
    Guid? EstudianteId { get; }
    bool IsAuthenticated { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface IJwtTokenGenerator
{
    string GenerateToken(Usuario usuario, string rol, Guid? empresaId = null, Guid? universidadId = null, Guid? estudianteId = null);
}

