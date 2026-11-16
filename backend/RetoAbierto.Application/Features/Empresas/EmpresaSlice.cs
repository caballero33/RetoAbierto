using FluentValidation;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Common.Models;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.Application.Features.Empresas.DTOs
{
    public class EmpresaResponse
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Correo { get; set; } = string.Empty;
        public bool Activa { get; set; }
        public string FechaRegistro { get; set; } = string.Empty;
    }

    public class ActualizarEmpresaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Correo { get; set; } = string.Empty;
    }

    public class UsuarioEmpresaResponse
    {
        public Guid UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string FechaIngreso { get; set; } = string.Empty;
    }

    public class CrearUsuarioEmpresaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public Guid RolId { get; set; }
    }

    public class UsuarioCreadoResponse
    {
        public Guid UsuarioId { get; set; }
        public string PasswordTemporal { get; set; } = string.Empty;
    }

    public class ActualizarUsuarioEmpresaRequest
    {
        public Guid? RolId { get; set; }
        public bool? Activo { get; set; }
    }

    public class RolResponse
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class CambiarEstadoEmpresaRequest
    {
        public bool Activa { get; set; }
    }
}

namespace RetoAbierto.Application.Features.Empresas.Validators
{
    using RetoAbierto.Application.Features.Empresas.DTOs;

    public class ActualizarEmpresaValidator : AbstractValidator<ActualizarEmpresaRequest>
    {
        public ActualizarEmpresaValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre de la empresa es obligatorio.")
                .MaximumLength(150).WithMessage("El nombre no debe superar los 150 caracteres.");

            RuleFor(x => x.Correo)
                .NotEmpty().WithMessage("El correo es obligatorio.")
                .EmailAddress().WithMessage("El correo no tiene un formato válido.");
        }
    }

    public class CrearUsuarioEmpresaValidator : AbstractValidator<CrearUsuarioEmpresaRequest>
    {
        public CrearUsuarioEmpresaValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(100).WithMessage("El nombre no debe superar los 100 caracteres.");

            RuleFor(x => x.Correo)
                .NotEmpty().WithMessage("El correo es obligatorio.")
                .EmailAddress().WithMessage("El formato del correo es inválido.");

            RuleFor(x => x.RolId)
                .NotEmpty().WithMessage("El identificador de rol es obligatorio.");
        }
    }
}

namespace RetoAbierto.Application.Features.Empresas.Services
{
    using RetoAbierto.Application.Features.Empresas.DTOs;

    public interface IEmpresaService
    {
        Task<EmpresaResponse> ObtenerMiEmpresaAsync(Guid empresaId, CancellationToken ct = default);
        Task<EmpresaResponse> ActualizarMiEmpresaAsync(Guid empresaId, ActualizarEmpresaRequest request, CancellationToken ct = default);
        Task<List<UsuarioEmpresaResponse>> ListarUsuariosEmpresaAsync(Guid empresaId, CancellationToken ct = default);
        Task<UsuarioCreadoResponse> CrearUsuarioEmpresaAsync(Guid empresaId, CrearUsuarioEmpresaRequest request, CancellationToken ct = default);
        Task ActualizarUsuarioEmpresaAsync(Guid empresaId, Guid usuarioActualId, Guid usuarioObjetivoId, ActualizarUsuarioEmpresaRequest request, CancellationToken ct = default);
        Task<List<RolResponse>> ListarRolesAsync(CancellationToken ct = default);
        Task<PagedResult<EmpresaResponse>> ListarEmpresasAdminAsync(int pagina, int tamano, CancellationToken ct = default);
        Task CambiarEstadoEmpresaAdminAsync(Guid empresaId, bool activa, CancellationToken ct = default);
    }

    public class EmpresaService : IEmpresaService
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public EmpresaService(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public Task<EmpresaResponse> ObtenerMiEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        {
            var empresa = _context.Empresas.FirstOrDefault(e => e.Id == empresaId);
            if (empresa == null)
                throw new NotFoundException("Empresa no encontrada.");

            return Task.FromResult(new EmpresaResponse
            {
                Id = empresa.Id,
                Nombre = empresa.Nombre,
                Descripcion = empresa.Descripcion,
                Correo = empresa.Correo,
                Activa = empresa.Activa,
                FechaRegistro = empresa.FechaRegistro.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }

        public async Task<EmpresaResponse> ActualizarMiEmpresaAsync(Guid empresaId, ActualizarEmpresaRequest request, CancellationToken ct = default)
        {
            var empresa = _context.Empresas.FirstOrDefault(e => e.Id == empresaId);
            if (empresa == null)
                throw new NotFoundException("Empresa no encontrada.");

            if (_context.Empresas.Any(e => e.Id != empresaId && e.Correo.ToLower() == request.Correo.ToLower()))
                throw new ConflictException("Ya existe otra empresa con ese correo.");

            empresa.Nombre = request.Nombre;
            empresa.Descripcion = request.Descripcion;
            empresa.Correo = request.Correo;

            await _context.SaveChangesAsync(ct);

            return await ObtenerMiEmpresaAsync(empresaId, ct);
        }

        public Task<List<UsuarioEmpresaResponse>> ListarUsuariosEmpresaAsync(Guid empresaId, CancellationToken ct = default)
        {
            var usuarios = _context.EmpresaUsuarios
                .Where(eu => eu.EmpresaId == empresaId)
                .Select(eu => new UsuarioEmpresaResponse
                {
                    UsuarioId = eu.UsuarioId,
                    Nombre = eu.Usuario.Nombre,
                    Correo = eu.Usuario.Correo,
                    Rol = eu.Rol.Nombre,
                    Activo = eu.Activo,
                    FechaIngreso = eu.FechaIngreso.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToList();

            return Task.FromResult(usuarios);
        }

        public async Task<UsuarioCreadoResponse> CrearUsuarioEmpresaAsync(Guid empresaId, CrearUsuarioEmpresaRequest request, CancellationToken ct = default)
        {
            var rol = _context.Roles.FirstOrDefault(r => r.Id == request.RolId);
            if (rol == null)
                throw new NotFoundException("El rol especificado no existe.");

            var usuarioExistente = _context.Usuarios.FirstOrDefault(u => u.Correo.ToLower() == request.Correo.ToLower());
            if (usuarioExistente != null)
            {
                if (_context.EmpresaUsuarios.Any(eu => eu.EmpresaId == empresaId && eu.UsuarioId == usuarioExistente.Id))
                    throw new ConflictException("El usuario ya pertenece a esta empresa.");

                var relacion = new EmpresaUsuario
                {
                    EmpresaId = empresaId,
                    UsuarioId = usuarioExistente.Id,
                    RolId = rol.Id,
                    Activo = true,
                    FechaIngreso = DateTime.UtcNow
                };
                await _context.AddAsync(relacion, ct);
                await _context.SaveChangesAsync(ct);

                return new UsuarioCreadoResponse
                {
                    UsuarioId = usuarioExistente.Id,
                    PasswordTemporal = "[Usuario ya registrado previamente]"
                };
            }

            string passTemporal = "Empresa" + Guid.NewGuid().ToString("N")[..6] + "!";
            var nuevoUsuario = new Usuario
            {
                Nombre = request.Nombre,
                Correo = request.Correo,
                PasswordHash = _passwordHasher.Hash(passTemporal),
                Activo = true,
                DebeCambiarPassword = true,
                FechaRegistro = DateTime.UtcNow
            };
            await _context.AddAsync(nuevoUsuario, ct);

            var nuevaRelacion = new EmpresaUsuario
            {
                EmpresaId = empresaId,
                UsuarioId = nuevoUsuario.Id,
                RolId = rol.Id,
                Activo = true,
                FechaIngreso = DateTime.UtcNow
            };
            await _context.AddAsync(nuevaRelacion, ct);

            await _context.SaveChangesAsync(ct);

            return new UsuarioCreadoResponse
            {
                UsuarioId = nuevoUsuario.Id,
                PasswordTemporal = passTemporal
            };
        }

        public async Task ActualizarUsuarioEmpresaAsync(Guid empresaId, Guid usuarioActualId, Guid usuarioObjetivoId, ActualizarUsuarioEmpresaRequest request, CancellationToken ct = default)
        {
            var relacion = _context.EmpresaUsuarios
                .FirstOrDefault(eu => eu.EmpresaId == empresaId && eu.UsuarioId == usuarioObjetivoId);

            if (relacion == null)
                throw new NotFoundException("El usuario no pertenece a la empresa.");

            if (request.Activo.HasValue && !request.Activo.Value)
            {
                if (usuarioActualId == usuarioObjetivoId && relacion.Rol.Nombre == RolesConstantes.AdminEmpresa)
                {
                    var otrosAdmins = _context.EmpresaUsuarios
                        .Count(eu => eu.EmpresaId == empresaId && eu.Activo && eu.Rol.Nombre == RolesConstantes.AdminEmpresa && eu.UsuarioId != usuarioActualId);

                    if (otrosAdmins == 0)
                        throw new ConflictException("No puede desactivarse a sí mismo porque es el único administrador activo de la empresa.");
                }
                relacion.Activo = false;
            }
            else if (request.Activo.HasValue && request.Activo.Value)
            {
                relacion.Activo = true;
            }

            if (request.RolId.HasValue)
            {
                var rol = _context.Roles.FirstOrDefault(r => r.Id == request.RolId.Value);
                if (rol == null)
                    throw new NotFoundException("Rol no encontrado.");
                relacion.RolId = rol.Id;
            }

            await _context.SaveChangesAsync(ct);
        }

        public Task<List<RolResponse>> ListarRolesAsync(CancellationToken ct = default)
        {
            var roles = _context.Roles
                .Select(r => new RolResponse
                {
                    Id = r.Id,
                    Nombre = r.Nombre,
                    Descripcion = r.Descripcion
                }).ToList();

            return Task.FromResult(roles);
        }

        public Task<PagedResult<EmpresaResponse>> ListarEmpresasAdminAsync(int pagina, int tamano, CancellationToken ct = default)
        {
            int total = _context.Empresas.Count();
            var items = _context.Empresas
                .OrderByDescending(e => e.FechaRegistro)
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .Select(e => new EmpresaResponse
                {
                    Id = e.Id,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    Correo = e.Correo,
                    Activa = e.Activa,
                    FechaRegistro = e.FechaRegistro.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }).ToList();

            return Task.FromResult(new PagedResult<EmpresaResponse>(items, total, pagina, tamano));
        }

        public async Task CambiarEstadoEmpresaAdminAsync(Guid empresaId, bool activa, CancellationToken ct = default)
        {
            var empresa = _context.Empresas.FirstOrDefault(e => e.Id == empresaId);
            if (empresa == null)
                throw new NotFoundException("Empresa no encontrada.");

            empresa.Activa = activa;
            await _context.SaveChangesAsync(ct);
        }
    }
}

