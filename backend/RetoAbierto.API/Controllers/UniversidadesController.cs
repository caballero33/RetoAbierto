using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Features.Universidades.DTOs;
using RetoAbierto.Application.Features.Universidades.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UniversidadesController : ControllerBase
{
    private readonly IUniversidadService _universidadService;
    private readonly IValidator<CrearUniversidadRequest> _crearValidator;

    public UniversidadesController(
        IUniversidadService universidadService,
        IValidator<CrearUniversidadRequest> crearValidator)
    {
        _universidadService = universidadService;
        _crearValidator = crearValidator;
    }

    [HttpPost]
    [Authorize(Roles = RolesConstantes.AdminPlataforma)]
    public async Task<IActionResult> Crear([FromBody] CrearUniversidadRequest request)
    {
        var validation = await _crearValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var response = await _universidadService.CrearAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar()
    {
        var result = await _universidadService.ListarAsync();
        return Ok(result);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminPlataforma)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoUniversidadRequest request)
    {
        await _universidadService.CambiarEstadoAsync(id, request.Activa);
        return NoContent();
    }
}

