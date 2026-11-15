using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Asignaciones.DTOs;
using RetoAbierto.Application.Features.Asignaciones.Services;
using RetoAbierto.Application.Features.Evaluaciones.DTOs;
using RetoAbierto.Application.Features.Evaluaciones.Services;
using RetoAbierto.Application.Features.Seguimiento.DTOs;
using RetoAbierto.Application.Features.Seguimiento.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AsignacionesController : ControllerBase
{
    private readonly IAsignacionService _asignacionService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly IEvaluacionService _evaluacionService;
    private readonly ITenantService _tenantService;

    private readonly IValidator<CambiarEstadoAsignacionRequest> _cambiarEstadoValidator;
    private readonly IValidator<CrearMetaRequest> _crearMetaValidator;
    private readonly IValidator<RegistrarHorasRequest> _registrarHorasValidator;
    private readonly IValidator<CrearEvaluacionRequest> _crearEvaluacionValidator;

    public AsignacionesController(
        IAsignacionService asignacionService,
        ISeguimientoService seguimientoService,
        IEvaluacionService evaluacionService,
        ITenantService tenantService,
        IValidator<CambiarEstadoAsignacionRequest> cambiarEstadoValidator,
        IValidator<CrearMetaRequest> crearMetaValidator,
        IValidator<RegistrarHorasRequest> registrarHorasValidator,
        IValidator<CrearEvaluacionRequest> crearEvaluacionValidator)
    {
        _asignacionService = asignacionService;
        _seguimientoService = seguimientoService;
        _evaluacionService = evaluacionService;
        _tenantService = tenantService;

        _cambiarEstadoValidator = cambiarEstadoValidator;
        _crearMetaValidator = crearMetaValidator;
        _registrarHorasValidator = registrarHorasValidator;
        _crearEvaluacionValidator = crearEvaluacionValidator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar([FromQuery] string? estado)
    {
        var rol = _tenantService.Rol;
        Guid? empresaId = null;
        Guid? estudianteId = null;

        if (rol == RolesConstantes.AdminEmpresa || rol == RolesConstantes.Evaluador)
            empresaId = _tenantService.EmpresaId;
        else if (rol == RolesConstantes.Estudiante)
            estudianteId = _tenantService.EstudianteId;

        var list = await _asignacionService.ListarAsync(empresaId, estudianteId, estado);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var rol = _tenantService.Rol;
        Guid? empresaId = (rol == RolesConstantes.AdminEmpresa || rol == RolesConstantes.Evaluador) ? _tenantService.EmpresaId : null;
        Guid? estudianteId = (rol == RolesConstantes.Estudiante) ? _tenantService.EstudianteId : null;

        var response = await _asignacionService.ObtenerPorIdAsync(id, empresaId, estudianteId);
        return Ok(response);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoAsignacionRequest request)
    {
        var validation = await _cambiarEstadoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _asignacionService.CambiarEstadoAsync(empresaId, id, request.Estado);
        return NoContent();
    }

    [HttpGet("{id}/metas")]
    [Authorize]
    public async Task<IActionResult> ListarMetas(Guid id)
    {
        var list = await _seguimientoService.ListarMetasAsync(id);
        return Ok(list);
    }

    [HttpPost("{id}/metas")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> CrearMeta(Guid id, [FromBody] CrearMetaRequest request)
    {
        var validation = await _crearMetaValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _seguimientoService.CrearMetaAsync(empresaId, id, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id}/horas")]
    [Authorize]
    public async Task<IActionResult> ListarHoras(Guid id)
    {
        var list = await _seguimientoService.ListarHorasAsync(id);
        return Ok(list);
    }

    [HttpPost("{id}/horas")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> RegistrarHoras(Guid id, [FromBody] RegistrarHorasRequest request)
    {
        var validation = await _registrarHorasValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var response = await _seguimientoService.RegistrarHorasAsync(estudianteId, id, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("{id}/evaluaciones")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> Evaluar(Guid id, [FromBody] CrearEvaluacionRequest request)
    {
        var validation = await _crearEvaluacionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var response = await _evaluacionService.EvaluarAsync(empresaId, usuarioId, id, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{id}/evaluaciones")]
    [Authorize]
    public async Task<IActionResult> ListarEvaluaciones(Guid id)
    {
        var list = await _evaluacionService.ListarPorAsignacionAsync(id);
        return Ok(list);
    }
}

