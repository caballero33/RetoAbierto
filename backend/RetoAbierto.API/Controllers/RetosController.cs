using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Equipos.DTOs;
using RetoAbierto.Application.Features.Equipos.Services;
using RetoAbierto.Application.Features.Habilidades.DTOs;
using RetoAbierto.Application.Features.Habilidades.Services;
using RetoAbierto.Application.Features.Pagos.DTOs;
using RetoAbierto.Application.Features.Pagos.Services;
using RetoAbierto.Application.Features.Postulaciones.DTOs;
using RetoAbierto.Application.Features.Postulaciones.Services;
using RetoAbierto.Application.Features.Recompensas.DTOs;
using RetoAbierto.Application.Features.Recompensas.Services;
using RetoAbierto.Application.Features.Recomendaciones.Services;
using RetoAbierto.Application.Features.Retos.DTOs;
using RetoAbierto.Application.Features.Retos.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RetosController : ControllerBase
{
    private readonly IRetoService _retoService;
    private readonly IPagoService _pagoService;
    private readonly IRecompensaService _recompensaService;
    private readonly IEquipoService _equipoService;
    private readonly IPostulacionService _postulacionService;
    private readonly IHabilidadService _habilidadService;
    private readonly IMatchingService _matchingService;
    private readonly ITenantService _tenantService;

    private readonly IValidator<CrearRetoRequest> _crearRetoValidator;
    private readonly IValidator<ActualizarRetoRequest> _actualizarRetoValidator;
    private readonly IValidator<CrearPagoRequest> _crearPagoValidator;
    private readonly IValidator<CrearRecompensaRequest> _crearRecompensaValidator;
    private readonly IValidator<CrearEquipoRequest> _crearEquipoValidator;
    private readonly IValidator<CrearPostulacionRequest> _crearPostulacionValidator;
    private readonly IValidator<ActualizarHabilidadesRetoRequest> _habilidadesRetoValidator;

    public RetosController(
        IRetoService retoService,
        IPagoService pagoService,
        IRecompensaService recompensaService,
        IEquipoService equipoService,
        IPostulacionService postulacionService,
        IHabilidadService habilidadService,
        IMatchingService matchingService,
        ITenantService tenantService,
        IValidator<CrearRetoRequest> crearRetoValidator,
        IValidator<ActualizarRetoRequest> actualizarRetoValidator,
        IValidator<CrearPagoRequest> crearPagoValidator,
        IValidator<CrearRecompensaRequest> crearRecompensaValidator,
        IValidator<CrearEquipoRequest> crearEquipoValidator,
        IValidator<CrearPostulacionRequest> crearPostulacionValidator,
        IValidator<ActualizarHabilidadesRetoRequest> habilidadesRetoValidator)
    {
        _retoService = retoService;
        _pagoService = pagoService;
        _recompensaService = recompensaService;
        _equipoService = equipoService;
        _postulacionService = postulacionService;
        _habilidadService = habilidadService;
        _matchingService = matchingService;
        _tenantService = tenantService;

        _crearRetoValidator = crearRetoValidator;
        _actualizarRetoValidator = actualizarRetoValidator;
        _crearPagoValidator = crearPagoValidator;
        _crearRecompensaValidator = crearRecompensaValidator;
        _crearEquipoValidator = crearEquipoValidator;
        _crearPostulacionValidator = crearPostulacionValidator;
        _habilidadesRetoValidator = habilidadesRetoValidator;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar(
        [FromQuery] string? estado,
        [FromQuery] Guid? habilidadId,
        [FromQuery] string? q,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20)
    {
        var result = await _retoService.ListarAsync(_tenantService.EmpresaId, _tenantService.Rol, estado, habilidadId, q, pagina, tamano);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> ObtenerPorId(Guid id)
    {
        var response = await _retoService.ObtenerPorIdAsync(id, _tenantService.EmpresaId, _tenantService.Rol);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> Crear([FromBody] CrearRetoRequest request)
    {
        var validation = await _crearRetoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _retoService.CrearAsync(empresaId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] ActualizarRetoRequest request)
    {
        var validation = await _actualizarRetoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _retoService.ActualizarAsync(empresaId, id, request);
        return Ok(response);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoRetoRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _retoService.CambiarEstadoAsync(empresaId, id, request.Estado);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        await _retoService.EliminarAsync(empresaId, id);
        return NoContent();
    }

    [HttpPut("{id}/habilidades")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> ActualizarHabilidades(Guid id, [FromBody] ActualizarHabilidadesRetoRequest request)
    {
        var validation = await _habilidadesRetoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _habilidadService.ActualizarHabilidadesRetoAsync(empresaId, id, request);
        return Ok(response);
    }

    [HttpGet("{id}/recomendaciones")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> ObtenerRecomendaciones(Guid id, [FromQuery] int top = 10)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var result = await _matchingService.ObtenerRecomendacionesAsync(empresaId, id, top);
        return Ok(result);
    }

    [HttpGet("{retoId}/pagos")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> ListarPagos(Guid retoId)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var list = await _pagoService.ListarPorRetoAsync(empresaId, retoId);
        return Ok(list);
    }

    [HttpPost("{retoId}/pagos")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CrearPago(Guid retoId, [FromBody] CrearPagoRequest request)
    {
        var validation = await _crearPagoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _pagoService.CrearAsync(empresaId, retoId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{retoId}/recompensas")]
    [Authorize]
    public async Task<IActionResult> ListarRecompensas(Guid retoId)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var list = await _recompensaService.ListarPorRetoAsync(empresaId, retoId);
        return Ok(list);
    }

    [HttpPost("{retoId}/recompensas")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CrearRecompensa(Guid retoId, [FromBody] CrearRecompensaRequest request)
    {
        var validation = await _crearRecompensaValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var response = await _recompensaService.CrearAsync(empresaId, retoId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("{retoId}/equipos")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> CrearEquipo(Guid retoId, [FromBody] CrearEquipoRequest request)
    {
        var validation = await _crearEquipoValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var response = await _equipoService.CrearEquipoAsync(estudianteId, retoId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{retoId}/equipos")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> ListarEquipos(Guid retoId)
    {
        var list = await _equipoService.ListarPorRetoAsync(retoId);
        return Ok(list);
    }

    [HttpPost("{retoId}/postulaciones")]
    [Authorize(Roles = RolesConstantes.Estudiante)]
    public async Task<IActionResult> CrearPostulacion(Guid retoId, [FromBody] CrearPostulacionRequest request)
    {
        var validation = await _crearPostulacionValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var estudianteId = _tenantService.EstudianteId
            ?? throw new UnauthorizedAccessException("El usuario no tiene un perfil de estudiante activo.");

        var response = await _postulacionService.PostularAsync(estudianteId, retoId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{retoId}/postulaciones")]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> ListarPostulaciones(Guid retoId, [FromQuery] string? estado)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa.");

        var list = await _postulacionService.ListarPorRetoAsync(empresaId, retoId, estado);
        return Ok(list);
    }
}

