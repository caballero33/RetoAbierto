using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Empresas.DTOs;
using RetoAbierto.Application.Features.Empresas.Services;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpresaController : ControllerBase
{
    private readonly IEmpresaService _empresaService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<ActualizarEmpresaRequest> _actualizarEmpresaValidator;
    private readonly IValidator<CrearUsuarioEmpresaRequest> _crearUsuarioValidator;

    public EmpresaController(
        IEmpresaService empresaService,
        ITenantService tenantService,
        IValidator<ActualizarEmpresaRequest> actualizarEmpresaValidator,
        IValidator<CrearUsuarioEmpresaRequest> crearUsuarioValidator)
    {
        _empresaService = empresaService;
        _tenantService = tenantService;
        _actualizarEmpresaValidator = actualizarEmpresaValidator;
        _crearUsuarioValidator = crearUsuarioValidator;
    }

    [HttpGet]
    [Authorize(Roles = $"{RolesConstantes.AdminEmpresa},{RolesConstantes.Evaluador}")]
    public async Task<IActionResult> ObtenerMiEmpresa()
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa en su token.");

        var response = await _empresaService.ObtenerMiEmpresaAsync(empresaId);
        return Ok(response);
    }

    [HttpPut]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> ActualizarMiEmpresa([FromBody] ActualizarEmpresaRequest request)
    {
        var validation = await _actualizarEmpresaValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa en su token.");

        var response = await _empresaService.ActualizarMiEmpresaAsync(empresaId, request);
        return Ok(response);
    }

    [HttpGet("usuarios")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> ListarUsuarios()
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa en su token.");

        var list = await _empresaService.ListarUsuariosEmpresaAsync(empresaId);
        return Ok(list);
    }

    [HttpPost("usuarios")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> CrearUsuario([FromBody] CrearUsuarioEmpresaRequest request)
    {
        var validation = await _crearUsuarioValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa en su token.");

        var response = await _empresaService.CrearUsuarioEmpresaAsync(empresaId, request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("usuarios/{usuarioId}")]
    [Authorize(Roles = RolesConstantes.AdminEmpresa)]
    public async Task<IActionResult> ActualizarUsuario(Guid usuarioId, [FromBody] ActualizarUsuarioEmpresaRequest request)
    {
        var empresaId = _tenantService.EmpresaId
            ?? throw new UnauthorizedAccessException("El usuario no tiene una empresa activa en su token.");

        var usuarioActualId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        await _empresaService.ActualizarUsuarioEmpresaAsync(empresaId, usuarioActualId, usuarioId, request);
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public RolesController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar()
    {
        var list = await _empresaService.ListarRolesAsync();
        return Ok(list);
    }
}

[ApiController]
[Route("api/admin/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaService _empresaService;

    public EmpresasController(IEmpresaService empresaService)
    {
        _empresaService = empresaService;
    }

    [HttpGet]
    [Authorize(Roles = RolesConstantes.AdminPlataforma)]
    public async Task<IActionResult> Listar([FromQuery] int pagina = 1, [FromQuery] int tamano = 20)
    {
        var result = await _empresaService.ListarEmpresasAdminAsync(pagina, tamano);
        return Ok(result);
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = RolesConstantes.AdminPlataforma)]
    public async Task<IActionResult> CambiarEstado(Guid id, [FromBody] CambiarEstadoEmpresaRequest request)
    {
        await _empresaService.CambiarEstadoEmpresaAdminAsync(id, request.Activa);
        return NoContent();
    }
}

