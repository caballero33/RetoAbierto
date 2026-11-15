using System.Text.Json.Serialization;

namespace RetoAbierto.Application.Features.Auth.DTOs;

public class RegistroEmpresaRequest
{
    public string EmpresaNombre { get; set; } = string.Empty;
    public string? EmpresaDescripcion { get; set; }
    public string EmpresaCorreo { get; set; } = string.Empty;
    public string AdminNombre { get; set; } = string.Empty;
    public string AdminCorreo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegistroEmpresaResponse
{
    public Guid EmpresaId { get; set; }
    public Guid UsuarioId { get; set; }
}

public class LoginRequest
{
    public string Correo { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginUsuarioDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public Guid? EmpresaId { get; set; }
    public Guid? UniversidadId { get; set; }
    public Guid? EstudianteId { get; set; }
}

public class EmpresaItemDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public LoginUsuarioDto Usuario { get; set; } = new();
    public List<EmpresaItemDto> Empresas { get; set; } = new();
}

public class CambiarPasswordRequest
{
    public string PasswordActual { get; set; } = string.Empty;
    public string PasswordNueva { get; set; } = string.Empty;
    public string Confirmacion { get; set; } = string.Empty;
}

public class CambiarEmpresaRequest
{
    public Guid EmpresaId { get; set; }
}

