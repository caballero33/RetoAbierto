using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Estudiantes.DTOs;
using RetoAbierto.Application.Features.Estudiantes.Services;
using RetoAbierto.Application.Features.Habilidades.DTOs;
using RetoAbierto.Application.Features.Habilidades.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstudiantesController : ControllerBase
{
    private readonly IEstudianteService _estudianteService;
    private readonly IHabilidadService _habilidadService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<RegistrarEstudianteRequest> _registrarValidator;
    private readonly IValidator<ActualizarPerfilRequest> _actualizarValidator;
    private readonly IValidator<ActualizarHabilidadesEstudianteRequest> _habilidadesValidator;

    public EstudiantesController(
        IEstudianteService estudianteService,
        IHabilidadService habilidadService,
        ITenantService tenantService,
        IValidator<RegistrarEstudianteRequest> registrarValidator,
        IValidator<ActualizarPerfilRequest> actualizarValidator,
        IValidator<ActualizarHabilidadesEstudianteRequest> habilidadesValidator)
    {
        _estudianteService = estudianteService;
        _habilidadService = habilidadService;
        _tenantService = tenantService;
        _registrarValidator = registrarValidator;
        _actualizarValidator = actualizarValidator;
        _habilidadesValidator = habilidadesValidator;
    }

    [HttpPost]
    [Authorize(Roles = RolesConstantes.AdminUniversidad)]
    public async Task<IActionResult> Registrar([FromBody] RegistrarEstudianteRequest request)
    {
        var validation = await _registrarValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var universidadId = _tenantService.UniversidadId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una universidad asociada.");

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var response = await _estudianteService.RegistrarAsync(universidadId, usuarioId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [Authorize(Roles = RolesConstantes.AdminUniversidad)]
    public async Task<IActionResult> Listar([FromQuery] bool? activo, [FromQuery] string? q, [FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
    {
        var universidadId = _tenantService.UniversidadId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una universidad asociada.");

        var result = await _estudianteService.ListarPorUniversidadAsync(universidadId, activo, q, pagina, tamano);
        return Ok(result);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminUniversidad)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoEstudianteRequest request)
    {
        var universidadId = _tenantService.UniversidadId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una universidad asociada.");

        await _estudianteService.CambiarEstadoAsync(universidadId, id, request.Activo);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> ObtenerMiPerfil()
    {
        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var perfil = await _estudianteService.ObtenerMiPerfilAsync(usuarioId);
        return Ok(perfil);
    }

    [HttpPut("me")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> ActualizarMiPerfil([FromBody] ActualizarPerfilRequest request)
    {
        var validation = await _actualizarValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var perfil = await _estudianteService.ActualizarMiPerfilAsync(usuarioId, request);
        return Ok(perfil);
    }

    [HttpPut("me/habilidades")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> ActualizarMisHabilidades([FromBody] ActualizarHabilidadesEstudianteRequest request)
    {
        var validation = await _habilidadesValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var perfil = await _habilidadService.ActualizarHabilidadesEstudianteAsync(usuarioId, request);
        return Ok(perfil);
    }
}

