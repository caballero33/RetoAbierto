using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RetoAbierto.Application.Common.Interfaces;

namespace RetoAbierto.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? EmpresaId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("empresaId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var guid))
                return guid;
            return null;
        }
    }

    public Guid? UsuarioId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var guid))
                return guid;
            return null;
        }
    }

    public string? Rol => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirst("rol")?.Value;

    public Guid? UniversidadId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("universidadId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var guid))
                return guid;
            return null;
        }
    }

    public Guid? EstudianteId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("estudianteId")?.Value;
            if (!string.IsNullOrEmpty(claim) && Guid.TryParse(claim, out var guid))
                return guid;
            return null;
        }
    }
}

