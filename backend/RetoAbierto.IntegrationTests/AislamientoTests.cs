using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RetoAbierto.Application.Common.Exceptions;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Application.Features.Auth.DTOs;
using RetoAbierto.Application.Features.Auth.Services;
using RetoAbierto.Application.Features.Equipos.DTOs;
using RetoAbierto.Application.Features.Equipos.Services;
using RetoAbierto.Application.Features.Postulaciones.DTOs;
using RetoAbierto.Application.Features.Postulaciones.Services;
using RetoAbierto.Application.Features.Retos.Services;
using RetoAbierto.Infrastructure.Data;
using RetoAbierto.Infrastructure.Services;
using RetoAbierto.Domain.Common;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;

namespace RetoAbierto.IntegrationTests;

public class AislamientoTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("incorrecto", true)]
    [InlineData("00000000-0000-0000-0000-000000000000", true)]
    [InlineData("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", false)]
    // Antes el identificador vacío o de identidad no autenticada podía aceptarse como empresa.
    // Estos casos comprueban que ahora se devuelve null; no prueban la validación HTTP del JWT.
    public void Tenant_rechaza_empresa_invalida_o_identidad_no_autenticada(string? claim, bool autenticado)
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            claim == null ? [] : new[] { new Claim("empresaId", claim) }, autenticado ? "prueba" : null));
        Assert.Null(new TenantService(accessor).EmpresaId);
    }

    [Fact]
    // La corrección no debe quitar filtros ni rediseñar tablas. Se comprueba el modelo de EF,
    // no la existencia física de esas tablas en un servidor PostgreSQL.
    public void Las_14_entidades_tienen_filtro_y_el_modelo_conserva_23_tablas()
    {
        using var f = new Escenario();
        using var db = f.Contexto();
        Assert.Equal(23, db.Model.GetEntityTypes().Count());
        var empresas = db.Model.GetEntityTypes().Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)).ToList();
        Assert.Equal(14, empresas.Count);
        Assert.All(empresas, e => Assert.NotNull(e.GetQueryFilter()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    // Se ejecuta para A y para B: además de contar filas, se comprueba su empresa propietaria.
    // Esto detecta que el contexto devuelva registros del otro tenant o reutilice una empresa incorrecta.
    public void Empresa_solo_ve_sus_14_tipos_de_registros(bool empresaA)
    {
        using var f = new Escenario();
        f.Empresa(empresaA ? f.A.Id : f.B.Id);
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        var esperada = empresaA ? f.A.Id : f.B.Id;
        Assert.All(api.Retos.ToList(), e => Assert.Equal(esperada, e.EmpresaId));
        Assert.Equal(2, api.Retos.Count());
        Assert.Equal(1, api.EmpresaUsuarios.Count());
        Assert.Equal(1, api.Pagos.Count());
        Assert.Equal(1, api.Recompensas.Count());
        Assert.Equal(1, api.RecompensaEntregas.Count());
        Assert.Equal(1, api.Equipos.Count());
        Assert.Equal(1, api.EquipoIntegrantes.Count());
        Assert.Equal(1, api.Postulaciones.Count());
        Assert.Equal(1, api.Asignaciones.Count());
        Assert.Equal(1, api.Metas.Count());
        Assert.Equal(1, api.Avances.Count());
        Assert.Equal(1, api.RegistroHoras.Count());
        Assert.Equal(1, api.Evaluaciones.Count());
        Assert.Equal(1, api.RetoHabilidades.Count());
        IEnumerable<ITenantEntity>[] colecciones = [api.EmpresaUsuarios.ToList(), api.Pagos.ToList(),
            api.Recompensas.ToList(), api.RecompensaEntregas.ToList(), api.Equipos.ToList(),
            api.EquipoIntegrantes.ToList(), api.Postulaciones.ToList(), api.Asignaciones.ToList(),
            api.Metas.ToList(), api.Avances.ToList(), api.RegistroHoras.ToList(), api.Evaluaciones.ToList(), api.RetoHabilidades.ToList()];
        Assert.All(colecciones, filas => Assert.All(filas, fila => Assert.Equal(esperada, fila.EmpresaId)));
        Assert.Null(api.Retos.SingleOrDefault(r => r.Id == (empresaA ? f.PrivadoB.Id : f.PrivadoA.Id)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    // Reproduce la brecha anterior: la empresa nula abría todas las fila
    // Ahora ni un anónimo ni un rol empresarial sin empresa obtiene registros de estos conjuntos
    public void Sin_empresa_no_hay_acceso_privado(bool autenticado)
    {
        using var f = new Escenario();
        f.Actor(autenticado ? RolesConstantes.AdminEmpresa : null);
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        Assert.Empty(api.Retos);
        Assert.Empty(api.EmpresaUsuarios);
        Assert.Empty(api.Pagos);
        Assert.Empty(api.Equipos);
        Assert.Empty(api.Postulaciones);
        Assert.Empty(api.Asignaciones);
        Assert.Empty(api.Metas);
        Assert.Empty(api.Avances);
        Assert.Empty(api.RegistroHoras);
        Assert.Empty(api.Evaluaciones);
        Assert.Empty(api.Recompensas);
        Assert.Empty(api.RecompensaEntregas);
        Assert.Empty(api.RetoHabilidades);
        Assert.Empty(api.EquipoIntegrantes);
    }

    [Fact]
    // Cerrar filtros sin excepciones bloquearía al estudiante. Se comprueba que conserva
    // catálogo y participación propios, sin pagos ni equipos ajenos, usando el contexto y servicios
    public async Task Estudiante_conserva_catalogo_equipo_postulaciones_y_seguimiento_propios()
    {
        using var f = new Escenario();
        f.Alumno();
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        var catalogo = await new RetoService(api).ListarAsync(null, RolesConstantes.Estudiante, null, null, null, 1, 20);
        Assert.Equal(2, catalogo.Total);
        Assert.Single(api.Equipos);
        Assert.Equal(f.EquipoA.Id, api.Equipos.Single().Id);
        Assert.Single(api.Postulaciones);
        Assert.Single(api.Asignaciones);
        Assert.Single(api.Metas);
        Assert.Single(api.Avances);
        Assert.Single(api.RegistroHoras);
        Assert.Single(api.Evaluaciones);
        Assert.Equal(2, api.RetoHabilidades.Count());
        Assert.Empty(api.Pagos);
        Assert.Empty(api.Recompensas);
        Assert.Null(api.Equipos.SingleOrDefault(e => e.Id == f.EquipoB.Id));
        Assert.Empty(db.Retos); // El DbSet conserva el filtro empresarial incluso para estudiantes.
    }

    [Fact]
    // Un estudiante no pertenece a una sola empresa: puede participar en un reto abierto de B
    // Se prueba creación, postulación y retiro; la empresa procede del reto, no de un dato libre del alumno
    public async Task Estudiante_puede_crear_equipo_y_postular_a_empresa_B_sin_claim_de_empresa()
    {
        using var f = new Escenario();
        f.Alumno();
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        var equipo = await new EquipoService(api).CrearEquipoAsync(f.EstudianteA.Id, f.AbiertoB.Id,
            new CrearEquipoRequest { Nombre = "Equipo nuevo" });
        var postulacion = await new PostulacionService(api).PostularAsync(f.EstudianteA.Id, f.AbiertoB.Id,
            new CrearPostulacionRequest { EquipoId = equipo.Id, Propuesta = new string('x', 60) });
        Assert.Equal(f.B.Id, api.Postulaciones.Single(p => p.Id == postulacion.Id).EmpresaId);
        await new PostulacionService(api).RetirarAsync(f.EstudianteA.Id, postulacion.Id);
        Assert.Equal(PostulacionEstado.Retirada, api.Postulaciones.Single(p => p.Id == postulacion.Id).Estado);
    }

    [Fact]
    // Ocultar el otro equipo no debe desactivar la regla de pertenencia única por reto
    // Se comprueba el rechazo aunque el líder no pueda consultar ese equipo mediante su acceso normal
    public async Task Lider_no_puede_agregar_un_estudiante_que_ya_esta_en_otro_equipo_del_mismo_reto()
    {
        using var f = new Escenario();
        f.Empresa(f.A.Id);
        using (var preparar = f.Contexto())
        {
            var otroEquipo = new Equipo { EmpresaId = f.A.Id, RetoId = f.EquipoA.RetoId, Nombre = "Otro equipo" };
            preparar.Add(otroEquipo);
            preparar.Add(new EquipoIntegrante { EmpresaId = f.A.Id, EquipoId = otroEquipo.Id, EstudianteId = f.EstudianteB.Id, EsLider = true });
            preparar.SaveChanges();
        }
        f.Alumno();
        using var db = f.Contexto();
        await Assert.ThrowsAsync<ConflictException>(() => new EquipoService(db)
            .AgregarIntegranteAsync(f.EstudianteA.Id, f.EquipoA.Id, f.EstudianteB.Id));
    }

    [Fact]
    // Antes la universidad sin empresa podía quedar sin límite empresarial
    // Ahora se verifica seguimiento relacionado con sus alumnos, sin pagos ni propuestas privadas
    public void Universidad_solo_ve_seguimiento_relacionado_con_sus_estudiantes()
    {
        using var f = new Escenario();
        f.Actor(RolesConstantes.AdminUniversidad, universidad: f.UniversidadA.Id);
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        Assert.Equal(2, api.Retos.Count());
        Assert.Single(api.Asignaciones);
        Assert.Single(api.Metas);
        Assert.Single(api.Evaluaciones);
        Assert.Equal(f.EstudianteA.Id, api.Avances.Single().EstudianteId);
        Assert.Equal(f.EstudianteA.Id, api.RegistroHoras.Single().EstudianteId);
        Assert.Empty(api.Pagos);
        Assert.Empty(api.Postulaciones);
    }

    [Fact]
    // Documenta la decisión confirmada: plataforma administra entidades globales,
    // pero la empresa nula no le da acceso a retos, pagos, postulaciones o asignaciones privadas
    public void Plataforma_conserva_gestion_global_sin_datos_transaccionales_privados()
    {
        using var f = new Escenario();
        f.Actor(RolesConstantes.AdminPlataforma);
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        Assert.Empty(api.Retos);
        Assert.Empty(api.Asignaciones);
        Assert.Empty(api.Pagos);
        Assert.Empty(api.Postulaciones);
        Assert.Equal(2, api.Empresas.Count());
        Assert.Equal(2, api.Universidades.Count());
        Assert.Single(api.Roles);
        Assert.Single(api.Habilidades);
    }

    [Theory]
    [InlineData(RolesConstantes.AdminUniversidad)]
    [InlineData(RolesConstantes.AdminPlataforma)]
    [InlineData("Desconocido")]
    // Tener empresa en el token no basta: también debe existir el rol autorizado para operarla
    // Estos casos evitan convertir roles universitarios, de plataforma o desconocidos en roles empresariales
    public void Un_claim_empresa_no_concede_permisos_empresariales_a_otros_roles(string rol)
    {
        using var f = new Escenario();
        f.Actor(rol, empresa: f.A.Id);
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        Assert.Empty(api.Pagos);
        Assert.Empty(api.Postulaciones);
        Assert.Empty(api.EmpresaUsuarios);
        Assert.Empty(api.Asignaciones);
    }

    [Theory]
    [InlineData("update", false)]
    [InlineData("delete", true)]
    [InlineData("insert", true)]
    [InlineData("spoof", true)]
    // Los filtros no impiden por sí solos adjuntar una fila ajena y guardarla
    // Se prueban alta, cambio, eliminación y dueño falsificado; después se verifica que B no cambió
    // Los casos ejercitan tanto el guardado síncrono como el asíncrono
    public async Task Empresa_no_puede_escribir_en_B_ni_falsificar_su_empresa(string ataque, bool asincrono)
    {
        using var f = new Escenario();
        f.Empresa(f.A.Id);
        using (var db = f.Contexto())
        {
            if (ataque == "insert") db.Retos.Add(new Reto { EmpresaId = f.B.Id, Titulo = "Intruso" });
            else
            {
                var fila = new Reto { Id = f.PrivadoB.Id, EmpresaId = ataque == "spoof" ? f.A.Id : f.B.Id, Titulo = "Intruso" };
                db.Attach(fila);
                if (ataque == "delete") db.Remove(fila);
                else db.Entry(fila).Property(r => r.Titulo).IsModified = true;
            }
            if (asincrono) await Assert.ThrowsAsync<ForbiddenException>(() => db.SaveChangesAsync());
            else Assert.Throws<ForbiddenException>(() => db.SaveChanges());
        }
        f.Empresa(f.B.Id);
        using var verificar = f.Contexto();
        Assert.Equal("Privado B", verificar.Retos.Single(r => r.Id == f.PrivadoB.Id).Titulo);
        Assert.Equal(2, verificar.Retos.Count());
    }

    [Fact]
    // La protección debe permitir editar lo propio, pero rechazar un pago de A ligado a un reto de B
    // Esto comprueba el control de relaciones en SaveChanges, no una nueva llave compuesta en PostgreSQL
    public async Task Empresa_puede_editar_lo_propio_pero_no_relacionarlo_con_un_reto_ajeno()
    {
        using var f = new Escenario();
        f.Empresa(f.A.Id);
        using var db = f.Contexto();
        db.Retos.Single(r => r.Id == f.PrivadoA.Id).Titulo = "Actualizado";
        await db.SaveChangesAsync();
        db.Pagos.Add(new Pago { EmpresaId = f.A.Id, RetoId = f.AbiertoB.Id, Monto = 10 });
        await Assert.ThrowsAsync<ForbiddenException>(() => db.SaveChangesAsync());
    }

    [Fact]
    // Ver un reto abierto no significa poder editarlo. Tampoco conocer una postulación ajena permite retirarla
    // Se comprueba que ambos intentos se rechazan al guardar con identidad estudiantil
    public async Task Estudiante_no_modifica_retos_ni_postulaciones_ajenas()
    {
        using var f = new Escenario();
        f.Alumno();
        using var db = f.Contexto();
        var reto = ((IApplicationDbContext)db).Retos.Single(r => r.Id == f.AbiertoB.Id);
        reto.Titulo = "Intruso";
        await Assert.ThrowsAsync<ForbiddenException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var ajena = new Postulacion { Id = f.PostulacionB.Id, EmpresaId = f.B.Id,
            EquipoId = f.EquipoB.Id, RetoId = f.AbiertoB.Id, Estado = PostulacionEstado.Retirada };
        db.Attach(ajena);
        db.Entry(ajena).Property(p => p.Estado).IsModified = true;
        await Assert.ThrowsAsync<ForbiddenException>(() => db.SaveChangesAsync());
    }

    [Fact]
    // La empresa es opcional para estos perfiles: endurecer sus filtros no debe impedirles iniciar sesión
    // Se verifican los roles e identificadores emitidos por AuthService, sin iniciar el servidor HTTP
    public async Task Login_estudiantil_y_universitario_no_necesitan_empresa()
    {
        using var f = new Escenario();
        f.Actor(null);
        using var db = f.Contexto();
        var alumno = await f.Auth(db).LoginAsync(new LoginRequest { Correo = "alumnoa@test.local", Password = Escenario.Password });
        Assert.Equal(RolesConstantes.Estudiante, alumno.Usuario.Rol);
        Assert.Equal(f.EstudianteA.Id, alumno.Usuario.EstudianteId);
        Assert.Null(alumno.Usuario.EmpresaId);
        var usuarioUni = new Usuario { Nombre = "Universidad", Correo = "universidad@test.local", PasswordHash = f.Usuario.PasswordHash };
        db.Add(usuarioUni);
        db.Add(new UniversidadUsuario { UniversidadId = f.UniversidadA.Id, UsuarioId = usuarioUni.Id });
        await db.SaveChangesAsync();
        var uni = await f.Auth(db).LoginAsync(new LoginRequest { Correo = usuarioUni.Correo, Password = Escenario.Password });
        Assert.Equal(RolesConstantes.AdminUniversidad, uni.Usuario.Rol);
        Assert.Equal(f.UniversidadA.Id, uni.Usuario.UniversidadId);
        Assert.Null(uni.Usuario.EmpresaId);
    }

    [Fact]
    // Un equipo puede reunir alumnos de dos universidades. Se verifica que acceder a su asignación
    // no abra a una universidad las horas y avances del alumno de la otra
    public void Universidad_no_ve_horas_de_otras_universidades_en_un_equipo_compartido()
    {
        using var f = new Escenario();
        f.Empresa(f.A.Id);
        using (var preparar = f.Contexto())
        {
            preparar.EquipoIntegrantes.Add(new EquipoIntegrante
            { EmpresaId = f.A.Id, EquipoId = f.EquipoA.Id, EstudianteId = f.EstudianteB.Id });
            var asignacion = preparar.Asignaciones.Single();
            var meta = preparar.Metas.Single();
            preparar.RegistroHoras.Add(new RegistroHoras
            { EmpresaId = f.A.Id, AsignacionId = asignacion.Id, EstudianteId = f.EstudianteB.Id, Horas = 1 });
            preparar.Avances.Add(new Avance
            { EmpresaId = f.A.Id, MetaId = meta.Id, EstudianteId = f.EstudianteB.Id, Descripcion = "Otro alumno" });
            preparar.SaveChanges();
        }
        f.Actor(RolesConstantes.AdminUniversidad, universidad: f.UniversidadA.Id);
        using var db = f.Contexto();
        Assert.Single(((IApplicationDbContext)db).Asignaciones);
        Assert.Equal(f.EstudianteA.Id, ((IApplicationDbContext)db).RegistroHoras.Single().EstudianteId);
        Assert.Equal(f.EstudianteA.Id, ((IApplicationDbContext)db).Avances.Single().EstudianteId);
    }

    [Fact]
    // Limitar todo a retos abiertos impediría seguir trabajando cuando pasan a EnCurso
    // Se prueba registrar avances y horas propios; este caso detectó la relación Usuario no cargada
    public async Task Seguimiento_estudiantil_sigue_funcionando_cuando_el_reto_ya_no_esta_abierto()
    {
        using var f = new Escenario();
        f.Empresa(f.A.Id);
        using (var preparar = f.Contexto())
        {
            preparar.Retos.Single(r => r.Estado == RetoEstado.Abierto).Estado = RetoEstado.EnCurso;
            preparar.SaveChanges();
        }
        f.Alumno();
        using var db = f.Contexto();
        var api = (IApplicationDbContext)db;
        var asignacion = api.Asignaciones.Single();
        var service = new RetoAbierto.Application.Features.Seguimiento.Services.SeguimientoService(api);
        await service.RegistrarAvanceAsync(f.EstudianteA.Id, api.Metas.Single().Id,
            new RetoAbierto.Application.Features.Seguimiento.DTOs.RegistrarAvanceRequest { Descripcion = "Progreso" });
        await service.RegistrarHorasAsync(f.EstudianteA.Id, asignacion.Id,
            new RetoAbierto.Application.Features.Seguimiento.DTOs.RegistrarHorasRequest { Fecha = DateTime.UtcNow, Horas = 1 });
        Assert.Equal(2, api.Avances.Count());
        Assert.Equal(2, api.RegistroHoras.Count());
    }

    [Fact]
    // Cerrar EmpresaUsuarios podría ocultar B mientras el token tiene A
    // Se verifican membresías, cambio autorizado, claim emitido y rechazo de una empresa ajena
    // Leer ese claim no sustituye una prueba de validación del token por el middleware HTTP
    public async Task Login_y_cambio_de_empresa_conservan_membresias_y_claim_correcto()
    {
        using var f = new Escenario();
        f.Actor(null);
        using var db = f.Contexto();
        var auth = f.Auth(db);
        var login = await auth.LoginAsync(new LoginRequest { Correo = f.Usuario.Correo, Password = Escenario.Password });
        Assert.Equal(2, login.Empresas.Count);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(new LoginRequest { Correo = f.Usuario.Correo, Password = "incorrecta" }));
        f.Empresa(f.A.Id);
        var cambiado = await auth.CambiarEmpresaAsync(f.Usuario.Id, f.B.Id);
        Assert.Equal(f.B.Id, cambiado.Usuario.EmpresaId);
        Assert.Equal(f.B.Id.ToString(), new JwtSecurityTokenHandler().ReadJwtToken(cambiado.Token).Claims.Single(c => c.Type == "empresaId").Value);
        await Assert.ThrowsAsync<ForbiddenException>(() => auth.CambiarEmpresaAsync(f.Usuario.Id, Guid.NewGuid()));
    }

    [Fact]
    // Una empresa nueva todavía no tiene contexto empresarial. Se comprueba la excepción limitada
    // que permite crear empresa, administrador y vínculo juntos, y después iniciar sesión
    public async Task Registro_de_empresa_funciona_sin_empresa_previa()
    {
        using var f = new Escenario();
        f.Actor(null);
        using var db = f.Contexto();
        var registro = await f.Auth(db).RegistroEmpresaAsync(new RegistroEmpresaRequest
        {
            EmpresaNombre = "Nueva", EmpresaCorreo = "nueva@test.local", AdminNombre = "Admin",
            AdminCorreo = "nuevo@test.local", Password = Escenario.Password
        });
        var login = await f.Auth(db).LoginAsync(new LoginRequest { Correo = "nuevo@test.local", Password = Escenario.Password });
        Assert.Equal(registro.EmpresaId, login.Usuario.EmpresaId);
    }

    [Fact]
    // SQLite no demuestra por sí solo compatibilidad con PostgreSQL
    // ToQueryString comprueba la traducción de Npgsql sin abrir conexión; falta ejecutar SQL en ese motor
    public void PostgreSQL_traduce_consultas_sin_conectarse_a_una_base()
    {
        using var f = new Escenario();
        foreach (var rol in new[] { RolesConstantes.AdminEmpresa, RolesConstantes.Estudiante, RolesConstantes.AdminUniversidad })
        {
            f.Actor(rol, f.A.Id, f.EstudianteA.Id, f.UniversidadA.Id);
            using var db = new RetoAbiertoDbContext(new DbContextOptionsBuilder<RetoAbiertoDbContext>()
                .UseNpgsql("Host=localhost;Database=NO_CONECTAR").Options, f.Tenant);
            var api = (IApplicationDbContext)db;
            Assert.Contains("SELECT", api.Retos.ToQueryString());
            Assert.Contains("SELECT", api.Asignaciones.ToQueryString());
            Assert.Contains("SELECT", api.Postulaciones.ToQueryString());
            Assert.Contains("SELECT", api.Avances.ToQueryString());
            Assert.Contains("SELECT", api.RegistroHoras.ToQueryString());
        }
    }

    private sealed class Escenario : IDisposable
    {
        // Cada escenario crea datos ficticios de dos empresas en SQLite en memoria
        // No se leen conexiones de appsettings ni se utiliza la base PostgreSQL del equipo
        public const string Password = "PruebaSegura123!";
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly HttpContextAccessor accessor = new() { HttpContext = new DefaultHttpContext() };
        public TenantService Tenant { get; }
        public Empresa A { get; } = new() { Nombre = "A", Correo = "a@test.local" };
        public Empresa B { get; } = new() { Nombre = "B", Correo = "b@test.local" };
        public Universidad UniversidadA { get; } = new() { Nombre = "U A", Correo = "ua@test.local" };
        private Universidad UniversidadB { get; } = new() { Nombre = "U B", Correo = "ub@test.local" };
        public Usuario Usuario { get; } = new() { Nombre = "Admin", Correo = "admin@test.local" };
        public Estudiante EstudianteA { get; } = new();
        public Estudiante EstudianteB { get; } = new();
        public Postulacion PostulacionB { get; private set; } = null!;
        public Reto AbiertoB { get; private set; } = null!;
        public Reto PrivadoA { get; private set; } = null!;
        public Reto PrivadoB { get; private set; } = null!;
        public Equipo EquipoA { get; private set; } = null!;
        public Equipo EquipoB { get; private set; } = null!;
        private readonly Rol rol = new() { Nombre = RolesConstantes.AdminEmpresa };
        private readonly Habilidad habilidad = new() { Nombre = "C#" };
        public Escenario()
        {
            connection.Open();
            Tenant = new TenantService(accessor);
            using var db = Contexto();
            // La base es SQLite en memoria, creada por prueba; nunca usa appsettings ni PostgreSQL
            db.Database.EnsureCreated();
            Usuario.PasswordHash = new PasswordHasher().Hash(Password);
            var cuentaA = new Usuario { Nombre = "Alumno A", Correo = "alumnoa@test.local", PasswordHash = Usuario.PasswordHash };
            var cuentaB = new Usuario { Nombre = "Alumno B", Correo = "alumnob@test.local", PasswordHash = Usuario.PasswordHash };
            EstudianteA.UsuarioId = cuentaA.Id; EstudianteA.UniversidadId = UniversidadA.Id; EstudianteA.RegistradoPorUsuarioId = Usuario.Id;
            EstudianteB.UsuarioId = cuentaB.Id; EstudianteB.UniversidadId = UniversidadB.Id; EstudianteB.RegistradoPorUsuarioId = Usuario.Id;
            db.AddRange(A, B, UniversidadA, UniversidadB, Usuario, cuentaA, cuentaB, EstudianteA, EstudianteB, rol, habilidad);
            db.SaveChanges();
            db.ChangeTracker.Clear();
            (PrivadoA, _, EquipoA) = Sembrar(db, A, EstudianteA);
            (PrivadoB, AbiertoB, EquipoB) = Sembrar(db, B, EstudianteB);
        }

        private (Reto, Reto, Equipo) Sembrar(RetoAbiertoDbContext db, Empresa empresa, Estudiante estudiante)
        {
            // Los datos empresariales se guardan con su empresa activa, sin desactivar la validación
            // Se limpia el seguimiento de EF al terminar para que las consultas prueben filas almacenadas
            Empresa(empresa.Id);
            var privado = new Reto { EmpresaId = empresa.Id, Titulo = "Privado " + empresa.Nombre };
            var abierto = new Reto { EmpresaId = empresa.Id, Titulo = "Abierto " + empresa.Nombre, Estado = RetoEstado.Abierto,
                MaxIntegrantes = 5, HorasEstimadas = 40, FechaLimite = DateTime.UtcNow.AddDays(20) };
            var equipo = new Equipo { EmpresaId = empresa.Id, RetoId = abierto.Id, Nombre = "Equipo " + empresa.Nombre };
            var post = new Postulacion { EmpresaId = empresa.Id, RetoId = abierto.Id, EquipoId = equipo.Id, Propuesta = "Propuesta" };
            if (empresa.Id == B.Id) PostulacionB = post;
            var asignacion = new Asignacion { EmpresaId = empresa.Id, PostulacionId = post.Id, FechaInicio = DateTime.UtcNow, FechaFin = DateTime.UtcNow.AddDays(10) };
            var meta = new Meta { EmpresaId = empresa.Id, AsignacionId = asignacion.Id, Titulo = "Meta" };
            var recompensa = new Recompensa { EmpresaId = empresa.Id, RetoId = abierto.Id, Descripcion = "Reconocimiento" };
            db.AddRange(privado, abierto, equipo, post, asignacion, meta, recompensa,
                new EmpresaUsuario { EmpresaId = empresa.Id, UsuarioId = Usuario.Id, RolId = rol.Id },
                new EquipoIntegrante { EmpresaId = empresa.Id, EquipoId = equipo.Id, EstudianteId = estudiante.Id, EsLider = true },
                new Pago { EmpresaId = empresa.Id, RetoId = abierto.Id, Monto = 10 },
                new RecompensaEntrega { EmpresaId = empresa.Id, RecompensaId = recompensa.Id, EstudianteId = estudiante.Id },
                new Avance { EmpresaId = empresa.Id, MetaId = meta.Id, EstudianteId = estudiante.Id, Descripcion = "Avance" },
                new RegistroHoras { EmpresaId = empresa.Id, AsignacionId = asignacion.Id, EstudianteId = estudiante.Id, Horas = 2 },
                new Evaluacion { EmpresaId = empresa.Id, AsignacionId = asignacion.Id, EvaluadoPorUsuarioId = Usuario.Id, Calificacion = 90 },
                new RetoHabilidad { EmpresaId = empresa.Id, RetoId = abierto.Id, HabilidadId = habilidad.Id, NivelRequerido = 2 });
            db.SaveChanges();
            db.ChangeTracker.Clear();
            return (privado, abierto, equipo);
        }

        public void Actor(string? rolActual, Guid? empresa = null, Guid? estudiante = null, Guid? universidad = null)
        {
            // Se simula el usuario que ya autenticó la API para probar permisos en la capa de datos.
            // Esta preparación no verifica firmas JWT ni reemplaza las pruebas HTTP pendientes.
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Usuario.Id.ToString()) };
            if (rolActual != null) claims.Add(new Claim(ClaimTypes.Role, rolActual));
            if (empresa.HasValue) claims.Add(new Claim("empresaId", empresa.Value.ToString()));
            if (estudiante.HasValue) claims.Add(new Claim("estudianteId", estudiante.Value.ToString()));
            if (universidad.HasValue) claims.Add(new Claim("universidadId", universidad.Value.ToString()));
            accessor.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity(claims, rolActual == null ? null : "prueba"));
        }
        public void Empresa(Guid id) => Actor(RolesConstantes.AdminEmpresa, empresa: id);
        public void Alumno() => Actor(RolesConstantes.Estudiante, estudiante: EstudianteA.Id, universidad: UniversidadA.Id);
        public RetoAbiertoDbContext Contexto() => new(new DbContextOptionsBuilder<RetoAbiertoDbContext>().UseSqlite(connection).Options, Tenant);
        public AuthService Auth(RetoAbiertoDbContext db) => new(db, new PasswordHasher(), new JwtTokenGenerator(new ConfigurationBuilder().Build()));
        public void Dispose() => connection.Dispose();
    }
}
