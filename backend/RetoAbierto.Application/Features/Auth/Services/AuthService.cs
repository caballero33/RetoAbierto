using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Auth.DTOs;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Auth.Services;

public interface IAuthService
{
    Task<RegistroEmpresaResponse> RegistroEmpresaAsync(RegistroEmpresaRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task CambiarPasswordAsync(Guid usuarioId, CambiarPasswordRequest request, CancellationToken ct = default);
    Task<LoginResponse> CambiarEmpresaAsync(Guid usuarioId, Guid empresaId, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<RegistroEmpresaResponse> RegistroEmpresaAsync(RegistroEmpresaRequest request, CancellationToken ct = default)
    {
        var existeEmpresaCorreo = _context.Empresas.Any(e => e.Correo.ToLower() == request.EmpresaCorreo.ToLower());
        if (existeEmpresaCorreo)
            throw new ConflictException("Ya existe una empresa registrada con ese correo.");

        var existeAdminCorreo = _context.Usuarios.Any(u => u.Correo.ToLower() == request.AdminCorreo.ToLower());
        if (existeAdminCorreo)
            throw new ConflictException("Ya existe un usuario registrado con ese correo de administrador.");

        var empresa = new Empresa
        {
            Nombre = request.EmpresaNombre,
            Descripcion = request.EmpresaDescripcion,
            Correo = request.EmpresaCorreo,
            Activa = true,
            FechaRegistro = DateTime.UtcNow
        };
        await _context.AddAsync(empresa, ct);

        var usuario = new Usuario
        {
            Nombre = request.AdminNombre,
            Correo = request.AdminCorreo,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Activo = true,
            DebeCambiarPassword = false,
            EsAdminPlataforma = false,
            FechaRegistro = DateTime.UtcNow
        };
        await _context.AddAsync(usuario, ct);

        var rolAdmin = _context.Roles.FirstOrDefault(r => r.Nombre == RolesConstantes.AdminEmpresa);
        if (rolAdmin == null)
        {
            rolAdmin = new Rol
            {
                Nombre = RolesConstantes.AdminEmpresa,
                Descripcion = "Administrador de Empresa"
            };
            await _context.AddAsync(rolAdmin, ct);
        }

        var empresaUsuario = new EmpresaUsuario
        {
            EmpresaId = empresa.Id,
            UsuarioId = usuario.Id,
            RolId = rolAdmin.Id,
            Activo = true,
            FechaIngreso = DateTime.UtcNow
        };
        await _context.AddAsync(empresaUsuario, ct);

        await _context.SaveChangesAsync(ct);

        return new RegistroEmpresaResponse
        {
            EmpresaId = empresa.Id,
            UsuarioId = usuario.Id
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo.ToLower() == request.Correo.ToLower());
        if (usuario == null || !usuario.Activo)
            throw new UnauthorizedAccessException("Credenciales inválidas o usuario inactivo.");

        if (!_passwordHasher.Verify(request.Password, usuario.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas o usuario inactivo.");

        string rol = RolesConstantes.Estudiante;
        Guid? empresaId = null;
        Guid? universidadId = null;
        Guid? estudianteId = null;

        var estudiante = _context.Estudiantes.FirstOrDefault(e => e.UsuarioId == usuario.Id && e.Activo);
        if (estudiante != null)
        {
            rol = RolesConstantes.Estudiante;
            estudianteId = estudiante.Id;
            universidadId = estudiante.UniversidadId;
        }

        // Correccion para conservar el login:
        // Antes se consultaba EmpresaUsuarios gracias al filtro abierto cuando no había empresa
        // Ahora, despues de verificar la contraseña, se consultan solo los vinculos de ese usuario
        // Esto permite elegir su empresa inicial sin abrir los datos empresariales al resto de usuarios
        var relacionesEmpresa = _context.MembresiasDeUsuario(usuario.Id)
            .Where(eu => eu.UsuarioId == usuario.Id && eu.Activo && eu.Empresa.Activa)
            .Select(eu => new { eu.EmpresaId, eu.Empresa.Nombre, RolNombre = eu.Rol.Nombre })
            .ToList();

        var listaEmpresas = relacionesEmpresa.Select(r => new EmpresaItemDto
        {
            Id = r.EmpresaId,
            Nombre = r.Nombre
        }).ToList();

        if (relacionesEmpresa.Any())
        {
            var primera = relacionesEmpresa.First();
            empresaId = primera.EmpresaId;
            rol = primera.RolNombre;
        }

        var relacionUni = _context.UniversidadUsuarios
            .FirstOrDefault(uu => uu.UsuarioId == usuario.Id && uu.Activo && uu.Universidad.Activa);
        if (relacionUni != null)
        {
            universidadId = relacionUni.UniversidadId;
            rol = RolesConstantes.AdminUniversidad;
        }

        if (usuario.EsAdminPlataforma)
        {
            rol = RolesConstantes.AdminPlataforma;
        }

        var token = _jwtTokenGenerator.GenerateToken(usuario, rol, empresaId, universidadId, estudianteId);
        var expiraEn = DateTime.UtcNow.AddHours(8);

        return new LoginResponse
        {
            Token = token,
            ExpiraEn = expiraEn,
            DebeCambiarPassword = usuario.DebeCambiarPassword,
            Usuario = new LoginUsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Rol = rol,
                EmpresaId = empresaId,
                UniversidadId = universidadId,
                EstudianteId = estudianteId
            },
            Empresas = listaEmpresas
        };
    }

    public async Task CambiarPasswordAsync(Guid usuarioId, CambiarPasswordRequest request, CancellationToken ct = default)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        if (usuario == null)
            throw new NotFoundException("Usuario no encontrado.");

        if (!_passwordHasher.Verify(request.PasswordActual, usuario.PasswordHash))
            throw new AppValidationException("passwordActual", "La contraseña actual no es correcta.");

        usuario.PasswordHash = _passwordHasher.Hash(request.PasswordNueva);
        usuario.DebeCambiarPassword = false;

        await _context.SaveChangesAsync(ct);
    }

    public async Task<LoginResponse> CambiarEmpresaAsync(Guid usuarioId, Guid empresaId, CancellationToken ct = default)
    {
        var usuario = _context.Usuarios.FirstOrDefault(u => u.Id == usuarioId);
        if (usuario == null || !usuario.Activo)
            throw new NotFoundException("Usuario no encontrado.");

        // Correcion del cambio de empresa:
        // El filtro de la empresa A ocultaría el vínculo con B aunque el usuario perteneciera a ambas.
        // Se buscan sus membresias activas y se exige que una coincida con la empresa solicitada.
        // Así puede cambiar a B, pero conocer el ID de una empresa ajena no permite cambiar a ella.
        var relacion = _context.MembresiasDeUsuario(usuario.Id)
            .FirstOrDefault(eu => eu.UsuarioId == usuarioId && eu.EmpresaId == empresaId && eu.Activo && eu.Empresa.Activa);

        if (relacion == null)
            throw new ForbiddenException("El usuario no pertenece a la empresa solicitada o la empresa está inactiva.");

        var rol = _context.Roles.FirstOrDefault(r => r.Id == relacion.RolId)?.Nombre ?? RolesConstantes.Evaluador;

        // Antes la lista podía quedar limitada por la empresa del token anterior.
        // Se devuelven las empresas activas del mismo usuario, sin incluir membresías de otros.
        var relacionesEmpresa = _context.MembresiasDeUsuario(usuario.Id)
            .Where(eu => eu.UsuarioId == usuario.Id && eu.Activo && eu.Empresa.Activa)
            .Select(eu => new EmpresaItemDto { Id = eu.EmpresaId, Nombre = eu.Empresa.Nombre })
            .ToList();

        var token = _jwtTokenGenerator.GenerateToken(usuario, rol, empresaId, null, null);

        return new LoginResponse
        {
            Token = token,
            ExpiraEn = DateTime.UtcNow.AddHours(8),
            DebeCambiarPassword = usuario.DebeCambiarPassword,
            Usuario = new LoginUsuarioDto
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Rol = rol,
                EmpresaId = empresaId,
                UniversidadId = null,
                EstudianteId = null
            },
            Empresas = relacionesEmpresa
        };
    }
}

