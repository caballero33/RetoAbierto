namespace RetoAbierto.Domain.Enums;

public static class RolesConstantes
{
    public const string AdminPlataforma = "AdminPlataforma";
    public const string AdminEmpresa = "AdminEmpresa";
    public const string Evaluador = "Evaluador";
    public const string AdminUniversidad = "AdminUniversidad";
    public const string Estudiante = "Estudiante";
}

public enum RetoEstado
{
    Borrador,
    Abierto,
    EnCurso,
    Cerrado,
    Cancelado
}

public enum PagoEstado
{
    Pendiente,
    Pagado,
    Anulado
}

public enum RecompensaEntregaEstado
{
    Pendiente,
    Entregada
}

public enum EquipoEstado
{
    Activo,
    Disuelto
}

public enum PostulacionEstado
{
    Enviada,
    EnRevision,
    Aceptada,
    Rechazada,
    Retirada
}

public enum AsignacionEstado
{
    EnCurso,
    Completada,
    Cancelada
}

public enum MetaEstado
{
    Pendiente,
    EnProgreso,
    Cumplida
}

public enum BecaEstado
{
    Pendiente,
    Validada,
    Rechazada,
    Finalizada
}

