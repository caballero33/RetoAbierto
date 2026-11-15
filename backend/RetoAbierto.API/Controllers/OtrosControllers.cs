using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Habilidades.DTOs;
using RetoAbierto.Application.Features.Habilidades.Services;
using RetoAbierto.Application.Features.Pagos.DTOs;
using RetoAbierto.Application.Features.Pagos.Services;
using RetoAbierto.Application.Features.Recompensas.DTOs;
using RetoAbierto.Application.Features.Recompensas.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagosController : ControllerBase
{
    private readonly IPagoService _pagoService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<CambiarEstadoPagoRequest> _cambiarEstadoValidator;

    public PagosController(
        IPagoService pagoService,
        ITenantService tenantService,
        IValidator<CambiarEstadoPagoRequest> cambiarEstadoValidator)
    {
        _pagoService = pagoService;
        _tenantService = tenantService;
        _cambiarEstadoValidator = cambiarEstadoValidator;
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoPagoRequest request)
    {
        var validation = await _cambiarEstadoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _pagoService.CambiarEstadoAsync(empresaId, id, request.Estado);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class RecompensasController : ControllerBase
{
    private readonly IRecompensaService _recompensaService;
    private readonly ITenantService _tenantService;

    public RecompensasController(
        IRecompensaService recompensaService,
        ITenantService tenantService)
    {
        _recompensaService = recompensaService;
        _tenantService = tenantService;
    }

    [HttpPost("{id}/entregas")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> RegistrarEntrega(Guid id, [FromBody] EntregarRecompensaRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _recompensaService.RegistrarEntregaAsync(empresaId, id, request.EstudianteId);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{id}/entregas/{estudianteId}")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CambiarEstadoEntrega(Guid id, Guid estudianteId, [FromBody] CambiarEstadoEntregaRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _recompensaService.CambiarEstadoEntregaAsync(empresaId, id, estudianteId, request.Estado);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class HabilidadesController : ControllerBase
{
    private readonly IHabilidadService _habilidadService;
    private readonly IValidator<CrearHabilidadRequest> _crearValidator;

    public HabilidadesController(
        IHabilidadService habilidadService,
        IValidator<CrearHabilidadRequest> crearValidator)
    {
        _habilidadService = habilidadService;
        _crearValidator = crearValidator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar([FromQuery] string? q)
    {
        var list = await _habilidadService.ListarAsync(q);
        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = RolesConstantes.AdminPlataforma)]
    public async Task<IActionResult> Crear([FromBody] CrearHabilidadRequest request)
    {
        var validation = await _crearValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var response = await _habilidadService.CrearAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}

