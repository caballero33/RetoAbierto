using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Infrastructure.Data;

public partial class RetoAbiertoDbContext
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        //  los filtros no revisan escrituras. Ahora se valida antes del guardado sincrono
        // El mismo control se aplica abajo al guardado asíncrono para no dejar un camino sin revisar
        ValidarEscrituras();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidarEscrituras();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private static ForbiddenException EscrituraDenegada() => new("La operación no está autorizada para esta empresa o participante.");

    private void ValidarEscrituras()
    {
        // Antes una entidad adjuntada podía guardarse sin pasar por una consulta filtrada
        // Se revisan las altas, cambios y eliminaciones de entidades empresariales antes de persistirlas
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is ITenantEntity
            && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            var entity = (ITenantEntity)entry.Entity;
            // El filtro no protege entidades adjuntadas ni INSERT/UPDATE/DELETE
            // Se contrasta con la fila real para impedir falsificar EmpresaId al adjuntar
            var persisted = entry.State == EntityState.Added ? null : entry.GetDatabaseValues();
            if (entity.EmpresaId == Guid.Empty || (entry.State != EntityState.Added && persisted == null)
                || (persisted != null && persisted.GetValue<Guid>(nameof(ITenantEntity.EmpresaId)) != entity.EmpresaId))
                throw EscrituraDenegada();

            // Comprobar el dueño de la fila no basta: sus relaciones también deben ser de esa empresa
            ValidarRelaciones(entry);
            if (EmpresaAutorizada && entity.EmpresaId == EmpresaActual)
                continue;

            // El registro inicial solo puede unir una empresa y un usuario nuevos del mismo guardado
            // Sin esta excepción, cerrar las escrituras impediría registrar una empresa antes de tener JWT
            // No permite agregar vínculos anónimos a empresas o usuarios que ya existian
            if (entry.State == EntityState.Added && entry.Entity is EmpresaUsuario eu
                && ChangeTracker.Entries<Empresa>().Any(e => e.State == EntityState.Added && e.Entity.Id == eu.EmpresaId)
                && ChangeTracker.Entries<Usuario>().Any(e => e.State == EntityState.Added && e.Entity.Id == eu.UsuarioId))
                continue;

            if (!EsEstudiante || !EscrituraDeParticipante(entry, persisted))
                throw EscrituraDenegada();
        }
    }

    private T Padre<T>(Guid id) where T : BaseEntity
    {
        // También se admite un padre nuevo del mismo guardado, por ejemplo al crear equipo e integrante
        // Para padres existentes se lee la fila almacenada, sin confiar en una copia adjuntada modificada
        var added = ChangeTracker.Entries<T>().FirstOrDefault(e => e.State == EntityState.Added && e.Entity.Id == id);
        // Una lectura sin filtros aquí solo valida integridad; nunca se devuelve a la API
        return added?.Entity ?? Set<T>().IgnoreQueryFilters().AsNoTracking().SingleOrDefault(e => e.Id == id)
            ?? throw EscrituraDenegada();
    }

    private void MismaEmpresa(ITenantEntity child, ITenantEntity parent)
    {
        // Antes las llaves simples podían aceptar un hijo de A relacionado con un padre de B
        // Se comparan sus empresas en la aplicación; esto no añade una restricción a PostgreSQL
        if (child.EmpresaId != parent.EmpresaId)
            throw EscrituraDenegada();
    }

    private void ValidarRelaciones(EntityEntry entry)
    {
        // Cada caso comprueba el padre inmediato: reto, equipo, postulación, asignación o meta
        // Así se revisa la correspondencia empresarial sin rediseñar las 23 tablas existentes
        var entity = (ITenantEntity)entry.Entity;
        switch (entry.Entity)
        {
            case Pago e: MismaEmpresa(entity, Padre<Reto>(e.RetoId)); break;
            case Recompensa e: MismaEmpresa(entity, Padre<Reto>(e.RetoId)); break;
            case RecompensaEntrega e: MismaEmpresa(entity, Padre<Recompensa>(e.RecompensaId)); break;
            case Equipo e: MismaEmpresa(entity, Padre<Reto>(e.RetoId)); break;
            case EquipoIntegrante e:
                var equipoIntegrante = Padre<Equipo>(e.EquipoId);
                MismaEmpresa(entity, equipoIntegrante);
                // La comprobación de duplicados debe ver también equipos que el líder no puede consultar
                // Al ocultar equipos ajenos, la consulta normal podría no detectar una pertenencia previa
                // Esta consulta interna conserva la regla de un equipo por reto, sin devolver el equipo ajeno
                if (entry.State == EntityState.Added && EquipoIntegrantes.IgnoreQueryFilters().Any(i =>
                    i.EstudianteId == e.EstudianteId && i.EquipoId != e.EquipoId && i.Equipo.RetoId == equipoIntegrante.RetoId))
                    throw new ConflictException("El estudiante ya pertenece a otro equipo para este mismo reto.");
                break;
            case RetoHabilidad e: MismaEmpresa(entity, Padre<Reto>(e.RetoId)); break;
            case Postulacion e:
                // No basta con que reto y equipo sean de la misma empresa: el equipo debe ser de ese reto
                // Se conserva esa correspondencia también al guardar entidades adjuntadas
                MismaEmpresa(entity, Padre<Reto>(e.RetoId));
                var equipo = Padre<Equipo>(e.EquipoId);
                MismaEmpresa(entity, equipo);
                if (equipo.RetoId != e.RetoId) throw EscrituraDenegada();
                break;
            case Asignacion e: MismaEmpresa(entity, Padre<Postulacion>(e.PostulacionId)); break;
            case Meta e: MismaEmpresa(entity, Padre<Asignacion>(e.AsignacionId)); break;
            case Avance e: MismaEmpresa(entity, Padre<Meta>(e.MetaId)); break;
            case RegistroHoras e: MismaEmpresa(entity, Padre<Asignacion>(e.AsignacionId)); break;
            case Evaluacion e: MismaEmpresa(entity, Padre<Asignacion>(e.AsignacionId)); break;
        }
    }

    // Antes se dependía del acceso amplio para comprobar al líder
    // Esta consulta interna solo responde si el estudiante autenticado lidera el equipo indicado
    private bool Lider(Guid equipoId) => EquipoIntegrantes.IgnoreQueryFilters().AsNoTracking()
        .Any(i => i.EquipoId == equipoId && i.EstudianteId == EstudianteActual && i.EsLider);

    private bool Participa(Guid asignacionId) => AsignacionesDelEstudiante().Contains(asignacionId);

    // Al retirar una postulación no se permite aprovechar el guardado para cambiar propuesta o equipo
    // Se compara con los valores persistidos y se aceptan únicamente los campos indicados
    private static bool SoloCambio(EntityEntry entry, PropertyValues persisted, params string[] permitidas) =>
        entry.Properties.All(p => permitidas.Contains(p.Metadata.Name) || Equals(p.CurrentValue, persisted[p.Metadata.Name]));

    private bool EscrituraDeParticipante(EntityEntry entry, PropertyValues? persisted)
    {
        // Estudiantes no reciben permiso empresarial general: únicamente estas operaciones
        switch (entry.Entity)
        {
            case Equipo e when entry.State == EntityState.Added:
                // Cerrar escrituras no debe impedir crear equipo en un reto abierto
                // Se exige que el mismo guardado incluya al estudiante como líder del nuevo equipo
                return Padre<Reto>(e.RetoId).Estado == RetoEstado.Abierto
                    && ChangeTracker.Entries<EquipoIntegrante>().Any(i => i.State == EntityState.Added
                        && i.Entity.EquipoId == e.Id && i.Entity.EstudianteId == EstudianteActual && i.Entity.EsLider);
            case EquipoIntegrante e:
                // El estudiante puede crear su vínculo de líder o gestionar integrantes como líder existente
                // No recibe permiso general para editar vínculos ni para eliminar al líder del equipo
                if (entry.State == EntityState.Added)
                {
                    var nuevoEquipo = ChangeTracker.Entries<Equipo>().Any(x => x.State == EntityState.Added && x.Entity.Id == e.EquipoId);
                    return nuevoEquipo ? e.EstudianteId == EstudianteActual && e.EsLider
                        : !e.EsLider && Lider(e.EquipoId) && Padre<Reto>(Padre<Equipo>(e.EquipoId).RetoId).Estado == RetoEstado.Abierto;
                }
                return entry.State == EntityState.Deleted && persisted != null && !persisted.GetValue<bool>(nameof(e.EsLider)) && Lider(e.EquipoId);
            case Postulacion e:
                // Se conservan las operaciones del líder: enviar una postulación o retirarla si no fue aceptada
                // SoloCambio impide modificar otros datos aprovechando la operación de retiro
                if (!Lider(e.EquipoId)) return false;
                if (entry.State == EntityState.Added)
                    return e.Estado == PostulacionEstado.Enviada && Padre<Reto>(e.RetoId).Estado == RetoEstado.Abierto;
                return entry.State == EntityState.Modified && persisted != null
                    && SoloCambio(entry, persisted, nameof(e.Estado)) && e.Estado == PostulacionEstado.Retirada
                    && persisted.GetValue<PostulacionEstado>(nameof(e.Estado)) != PostulacionEstado.Aceptada;
            case Avance e when entry.State == EntityState.Added:
                // El avance debe identificar al estudiante autenticado y una asignación en la que participa
                // Esto conserva su trabajo sin permitir registrar avances a nombre de otro estudiante
                return e.EstudianteId == EstudianteActual && Participa(Padre<Meta>(e.MetaId).AsignacionId);
            case RegistroHoras e when entry.State == EntityState.Added:
                // Las horas también deben ser propias y de una asignación donde participa
                // Las validaciones de fechas y cantidad siguen en el servicio existente
                return e.EstudianteId == EstudianteActual && Participa(e.AsignacionId);
            default: return false;
        }
    }
}
