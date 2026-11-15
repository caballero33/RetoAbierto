using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Equipos.DTOs;
using RetoAbierto.Application.Features.Equipos.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquiposController : ControllerBase
{
    private readonly IEquipoService _equipoService;
    private readonly ITenantService _tenantService;

    public EquiposController(IEquipoService equipoService, ITenantService tenantService)
    {
        _equipoService = equipoService;
        _tenantService = tenantService;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var response = await _equipoService.ObtenerPorIdAsync(id);
        return Ok(response);
    }

    [HttpPost("{id}/integrantes")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> AgregarIntegrante(Guid id, [FromBody] AgregarIntegranteRequest request)
    {
        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var response = await _equipoService.AgregarIntegranteAsync(estudianteId, id, request.EstudianteId);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("{id}/integrantes/{estudianteId}")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> EliminarIntegrante(Guid id, Guid estudianteId)
    {
        var liderEstudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        await _equipoService.EliminarIntegranteAsync(liderEstudianteId, id, estudianteId);
        return NoContent();
    }
}

