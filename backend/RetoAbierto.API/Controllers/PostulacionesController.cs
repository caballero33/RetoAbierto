using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Asignaciones.DTOs;
using RetoAbierto.Application.Features.Asignaciones.Services;
using RetoAbierto.Application.Features.Postulaciones.DTOs;
using RetoAbierto.Application.Features.Postulaciones.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostulacionesController : ControllerBase
{
    private readonly IPostulacionService _postulacionService;
    private readonly IAsignacionService _asignacionService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<CrearAsignacionRequest> _crearAsignacionValidator;

    public PostulacionesController(
        IPostulacionService postulacionService,
        IAsignacionService asignacionService,
        ITenantService tenantService,
        IValidator<CrearAsignacionRequest> crearAsignacionValidator)
    {
        _postulacionService = postulacionService;
        _asignacionService = asignacionService;
        _tenantService = tenantService;
        _crearAsignacionValidator = crearAsignacionValidator;
    }

    [HttpGet("mias")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> ListarMias()
    {
        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var list = await _postulacionService.ListarMiasAsync(estudianteId);
        return Ok(list);
    }

    [HttpPost("{id}/asignacion")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CrearAsignacion(Guid id, [FromBody] CrearAsignacionRequest request)
    {
        var validation = await _crearAsignacionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _asignacionService.CrearAsync(empresaId, id, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoPostulacionRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _postulacionService.CambiarEstadoAsync(empresaId, id, request.Estado);
        return NoContent();
    }

    [HttpPatch("{id}/retirar")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> Retirar(Guid id)
    {
        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        await _postulacionService.RetirarAsync(estudianteId, id);
        return NoContent();
    }
}

