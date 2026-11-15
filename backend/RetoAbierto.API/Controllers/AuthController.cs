using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Auth.DTOs;
using RetoAbierto.Application.Features.Auth.Services;

namespace RetoAbierto.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITenantService _tenantService;
    private readonly IValidator<RegistroEmpresaRequest> _registroValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<CambiarPasswordRequest> _cambiarPasswordValidator;

    public AuthController(
        IAuthService authService,
        ITenantService tenantService,
        IValidator<RegistroEmpresaRequest> registroValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<CambiarPasswordRequest> cambiarPasswordValidator)
    {
        _authService = authService;
        _tenantService = tenantService;
        _registroValidator = registroValidator;
        _loginValidator = loginValidator;
        _cambiarPasswordValidator = cambiarPasswordValidator;
    }

    [HttpPost("registro-empresa")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistroEmpresa([FromBody] RegistroEmpresaRequest request)
    {
        var validation = await _registroValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var response = await _authService.RegistroEmpresaAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("cambiar-password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
    {
        var validation = await _cambiarPasswordValidator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        await _authService.CambiarPasswordAsync(usuarioId, request);
        return NoContent();
    }

    [HttpPost("cambiar-empresa")]
    [Authorize]
    public async Task<IActionResult> CambiarEmpresa([FromBody] CambiarEmpresaRequest request)
    {
        var usuarioId = _tenantService.UsuarioId
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var response = await _authService.CambiarEmpresaAsync(usuarioId, request.EmpresaId);
        return Ok(response);
    }
}

