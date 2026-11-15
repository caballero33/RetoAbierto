using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Becas.DTOs;
using RetoAbierto.Application.Features.Becas.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BecasController : ControllerBase
{
    private readonly IBecaService _becaService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<CrearBecaRequest> _crearValidator;
    private readonly IValidator<CambiarEstadoBecaRequest> _cambiarEstadoValidator;

    public BecasController(
        IBecaService becaService,
        ITenantService tenantService,
        IValidator<CrearBecaRequest> crearValidator,
        IValidator<CambiarEstadoBecaRequest> cambiarEstadoValidator)
    {
        _becaService = becaService;
        _tenantService = tenantService;
        _crearValidator = crearValidator;
        _cambiarEstadoValidator = cambiarEstadoValidator;
    }

    [HttpPost]
    [Authorize(Roles = RolesConstantes.AdminUniversidad)]
    public async Task<IActionResult> Crear([FromBody] CrearBecaRequest request)
    {
        var validation = await _crearValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var universidadId = _tenantService.UniversidadId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una universidad asociada.");

        var response = await _becaService.CrearAsync(universidadId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar()
    {
        var rol = _tenantService.Rol;
        Guid? universidadId = null;
        Guid? estudianteId = null;

        if (rol == RolesConstantes.AdminUniversidad)
            universidadId = _tenantService.UniversidadId;
        else if (rol == RolesConstantes.Estudiante)
            estudianteId = _tenantService.EstudianteId;
        else if (rol != RolesConstantes.AdminPlataforma)
            return Ok(new List<BecaResponse>());

        var result = await _becaService.ListarAsync(universidadId, estudianteId);
        return Ok(result);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminUniversidad)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoBecaRequest request)
    {
        var validation = await _cambiarEstadoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var universidadId = _tenantService.UniversidadId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una universidad asociada.");

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        await _becaService.CambiarEstadoAsync(universidadId, usuarioId, id, request.Estado);
        return NoContent();
    }
}

