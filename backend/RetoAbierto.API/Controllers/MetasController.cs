using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Seguimiento.DTOs;
using RetoAbierto.Application.Features.Seguimiento.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetasController : ControllerBase
{
    private readonly ISeguimientoService _seguimientoService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<RegistrarAvanceRequest> _registrarAvanceValidator;

    public MetasController(
        ISeguimientoService seguimientoService,
        ITenantService tenantService,
        IValidator<RegistrarAvanceRequest> registrarAvanceValidator)
    {
        _seguimientoService = seguimientoService;
        _tenantService = tenantService;
        _registrarAvanceValidator = registrarAvanceValidator;
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoMetaRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _seguimientoService.CambiarEstadoMetaAsync(empresaId, id, request.Estado);
        return NoContent();
    }

    [HttpGet("{id}/avances")]
    [Authorize]
    public async Task<IActionResult> ListarAvances(Guid id)
    {
        var list = await _seguimientoService.ListarAvancesAsync(id);
        return Ok(list);
    }

    [HttpPost("{id}/avances")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> RegistrarAvance(Guid id, [FromBody] RegistrarAvanceRequest request)
    {
        var validation = await _registrarAvanceValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var response = await _seguimientoService.RegistrarAvanceAsync(estudianteId, id, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}

