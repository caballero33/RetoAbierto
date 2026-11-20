using Microsoft.EntityFrameworkCore;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Infrastructure.Data;

public partial class RetoAbiertoDbContext
{
    private Guid? EmpresaActual => _tenantService.EmpresaId;
    // Correccion: antes bastaba no tener empresa para saltarse el aislamiento
    // Ahora el acceso empresarial requiere autenticación, empresa valida y rol empresarial
    // Un claim de empresa por sí solo no convierte a universidad o plataforma en administradores de ella
    private bool EmpresaAutorizada => _tenantService.IsAuthenticated && EmpresaActual.HasValue
        && EmpresaActual != Guid.Empty
        && (_tenantService.Rol == RolesConstantes.AdminEmpresa || _tenantService.Rol == RolesConstantes.Evaluador);
    private Guid? EstudianteActual => _tenantService.EstudianteId;
    // Las excepciones requieren el rol y el identificador del perfil autenticado
    // No se interpretan identificadores ausentes como permiso para consultar a todos
    private bool EsEstudiante => _tenantService.IsAuthenticated
        && _tenantService.Rol == RolesConstantes.Estudiante
        && EstudianteActual.HasValue && EstudianteActual != Guid.Empty;
    private bool EsUniversidad => _tenantService.IsAuthenticated
        && _tenantService.Rol == RolesConstantes.AdminUniversidad
        && _tenantService.UniversidadId.HasValue && _tenantService.UniversidadId != Guid.Empty;

    IQueryable<EmpresaUsuario> IApplicationDbContext.MembresiasDeUsuario(Guid usuarioId)
    {
        // Antes login y cambio de empresa dependían del filtro abierto por empresa nula
        // Aquí se omite ese filtro únicamente para buscar vinculos activos del usuario indicado
        // La verificación de su identidad corresponde al servicio de autenticacion que llama este metodo
        // El login ya comprobó la contraseña el cambio de empresa usa el usuario del JWT
        return EmpresaUsuarios.IgnoreQueryFilters()
            .Where(e => e.UsuarioId == usuarioId && e.Activo && e.Empresa.Activa);
    }

    // Antes un estudiante sin empresa podia consultar equipos ajenos
    // Esta lista identifica únicamente los equipos donde figura como integrante y acota las consultas siguientes
    private IQueryable<Guid> EquiposDelEstudiante() => EquipoIntegrantes.IgnoreQueryFilters()
        .Where(i => i.EstudianteId == EstudianteActual).Select(i => i.EquipoId);

    // La asignación debe pertenecer a uno de sus equipos, aunque el reto ya no esté abierto
    // Esto conserva su seguimiento sin conceder acceso al resto de asignaciones de la empresa
    private IQueryable<Guid> AsignacionesDelEstudiante() => Asignaciones.IgnoreQueryFilters()
        .Where(a => EquiposDelEstudiante().Contains(a.Postulacion.EquipoId)).Select(a => a.Id);

    // El seguimiento universitario se basa en los alumnos de la universidad del usuario
    // Antes no tener empresa podía dejar visibles registros de alumnos de otras universidades
    private IQueryable<Guid> EstudiantesDeUniversidad() => Estudiantes
        .Where(e => e.UniversidadId == _tenantService.UniversidadId).Select(e => e.Id);

    // La universidad sigue asignaciones de sus alumnos; horas y avances además filtran al alumno
    // Se admite la asignación compartida solo si participa al menos uno de sus estudiantes
    // Es el alcance confirmado por la usuaria, no un permiso empresarial global
    private IQueryable<Guid> AsignacionesDeUniversidad() => Asignaciones.IgnoreQueryFilters()
        .Where(a => a.Postulacion.Equipo.Integrantes.Any(i => EstudiantesDeUniversidad().Contains(i.EstudianteId)))
        .Select(a => a.Id);

    // Las excepciones del estudiante se limitan por participación. Los DbSet públicos
    // conservan el filtro empresarial cerrado; Application recibe consultas acotadas
    // Antes cerrar el filtro habría ocultado también el catálogo a los estudiantes
    // Ahora ven retos abiertos y, en consultas internas de participación, sus retos vinculados
    // La universidad consulta solo abiertos aqui; el seguimiento usa la asignación autorizada
    // Include carga la empresa para respuestas que necesitan su nombre, sin depender de carga automática
    private IQueryable<Reto> ConsultaRetos() => EsEstudiante
        ? Retos.IgnoreQueryFilters().Where(r => r.Estado == RetoEstado.Abierto
            || EquipoIntegrantes.Any(i => i.EstudianteId == EstudianteActual && i.Equipo.RetoId == r.Id))
            .Include(r => r.Empresa)
        : EsUniversidad ? Retos.IgnoreQueryFilters().Where(r => r.Estado == RetoEstado.Abierto).Include(r => r.Empresa)
        : Retos.Include(r => r.Empresa);

    // Antes la empresa nula dejaba consultar cualquier equipo por ID
    // Ahora el estudiante solo consulta equipos propios; los demás usuarios conservan el filtro empresarial
    private IQueryable<Equipo> ConsultaEquipos() => EsEstudiante
        ? Equipos.IgnoreQueryFilters().Where(e => EquiposDelEstudiante().Contains(e.Id))
        : Equipos;

    // Se permiten los integrantes de sus equipos para consultar compañeros y comprobar liderazgo
    // No se abre el listado de integrantes de equipos ajenos
    private IQueryable<EquipoIntegrante> ConsultaEquipoIntegrantes() => EsEstudiante
        ? EquipoIntegrantes.IgnoreQueryFilters().Where(i => EquiposDelEstudiante().Contains(i.EquipoId))
        : EquipoIntegrantes;

    // Antes conocer el ID podía permitir leer una postulación ajena sin empresa activa
    // La consulta se limita a sus equipos; las escrituras comprueban además el liderazgo en Writes
    private IQueryable<Postulacion> ConsultaPostulaciones() => EsEstudiante
        ? Postulaciones.IgnoreQueryFilters().Where(p => EquiposDelEstudiante().Contains(p.EquipoId))
        : Postulaciones;

    private IQueryable<Asignacion> ConsultaAsignaciones()
    {
        // Antes los identificadores ausentes podian dejar esta consulta sin restricciones
        // Ahora estudiante y universidad reciben solo asignaciones relacionadas con su participacion
        var query = EsEstudiante
            ? Asignaciones.IgnoreQueryFilters().Where(a => AsignacionesDelEstudiante().Contains(a.Id))
            : EsUniversidad ? Asignaciones.IgnoreQueryFilters().Where(a => AsignacionesDeUniversidad().Contains(a.Id))
            : Asignaciones;
        // Se cargan las relaciones usadas por el detalle y la comprobación de participación.
        // Sin esta carga, consultar sus propiedades no garantiza que EF ya las haya recuperado.
        return query.Include(a => a.Postulacion).ThenInclude(p => p.Reto)
            .Include(a => a.Postulacion).ThenInclude(p => p.Equipo).ThenInclude(e => e.Integrantes);
    }

    // Antes bastaba el ID de la asignación con empresa nula para consultar sus metas.
    // Ahora la meta debe pertenecer a una asignación autorizada para ese estudiante o universidad.
    private IQueryable<Meta> ConsultaMetas() => EsEstudiante
        ? Metas.IgnoreQueryFilters().Where(m => AsignacionesDelEstudiante().Contains(m.AsignacionId))
        : EsUniversidad ? Metas.IgnoreQueryFilters().Where(m => AsignacionesDeUniversidad().Contains(m.AsignacionId))
        : Metas;

    // Los avances se limitan a asignaciones propias; la universidad ve además solo a sus alumnos.
    // Esto evita mostrar avances de otra universidad en un equipo con participantes de ambas.
    private IQueryable<Avance> ConsultaAvances() => EsEstudiante
        ? Avances.IgnoreQueryFilters().Where(a => AsignacionesDelEstudiante().Contains(a.Meta.AsignacionId))
        : EsUniversidad ? Avances.IgnoreQueryFilters().Where(a => EstudiantesDeUniversidad().Contains(a.EstudianteId)
            && AsignacionesDeUniversidad().Contains(a.Meta.AsignacionId))
        : Avances;

    // Antes la ausencia de empresa podía mostrar horas ajenas
    // Ahora se exige participacion; para universidad también se comprueba de quién son las horas
    private IQueryable<RegistroHoras> ConsultaRegistroHoras() => EsEstudiante
        ? RegistroHoras.IgnoreQueryFilters().Where(h => AsignacionesDelEstudiante().Contains(h.AsignacionId))
        : EsUniversidad ? RegistroHoras.IgnoreQueryFilters().Where(h => EstudiantesDeUniversidad().Contains(h.EstudianteId)
            && AsignacionesDeUniversidad().Contains(h.AsignacionId))
        : RegistroHoras;

    // La evaluacion es de la asignacion: se comparte solo con participantes o su universidad autorizada
    // No se usa la falta de empresa para abrir evaluaciones de todas las asignaciones
    private IQueryable<Evaluacion> ConsultaEvaluaciones() => EsEstudiante
        ? Evaluaciones.IgnoreQueryFilters().Where(e => AsignacionesDelEstudiante().Contains(e.AsignacionId))
        : EsUniversidad ? Evaluaciones.IgnoreQueryFilters().Where(e => AsignacionesDeUniversidad().Contains(e.AsignacionId))
        : Evaluaciones;

    // Cerrar el filtro también ocultaría las habilidades del catalogo.
    // Esta excepción acompaña el acceso al reto: abierto o vinculado al estudiante; universidad, solo abierto
    private IQueryable<RetoHabilidad> ConsultaRetoHabilidades() => EsEstudiante
        ? RetoHabilidades.IgnoreQueryFilters().Where(h => h.Reto.Estado == RetoEstado.Abierto
            || EquipoIntegrantes.Any(i => i.EstudianteId == EstudianteActual && i.Equipo.RetoId == h.RetoId))
        : EsUniversidad ? RetoHabilidades.IgnoreQueryFilters().Where(h => h.Reto.Estado == RetoEstado.Abierto)
        : RetoHabilidades;
}
