using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RetoAbierto.Application.Features.Auth.Services;
using RetoAbierto.Application.Features.Universidades.Services;
using RetoAbierto.Application.Features.Estudiantes.Services;
using RetoAbierto.Application.Features.Becas.Services;
using RetoAbierto.Application.Features.Empresas.Services;
using RetoAbierto.Application.Features.Retos.Services;
using RetoAbierto.Application.Features.Pagos.Services;
using RetoAbierto.Application.Features.Recompensas.Services;
using RetoAbierto.Application.Features.Equipos.Services;
using RetoAbierto.Application.Features.Postulaciones.Services;
using RetoAbierto.Application.Features.Asignaciones.Services;
using RetoAbierto.Application.Features.Seguimiento.Services;
using RetoAbierto.Application.Features.Evaluaciones.Services;
using RetoAbierto.Application.Features.Habilidades.Services;
using RetoAbierto.Application.Features.Recomendaciones.Services;

namespace RetoAbierto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUniversidadService, UniversidadService>();
        services.AddScoped<IEstudianteService, EstudianteService>();
        services.AddScoped<IBecaService, BecaService>();
        services.AddScoped<IEmpresaService, EmpresaService>();
        services.AddScoped<IRetoService, RetoService>();
        services.AddScoped<IPagoService, PagoService>();
        services.AddScoped<IRecompensaService, RecompensaService>();
        services.AddScoped<IEquipoService, EquipoService>();
        services.AddScoped<IPostulacionService, PostulacionService>();
        services.AddScoped<IAsignacionService, AsignacionService>();
        services.AddScoped<ISeguimientoService, SeguimientoService>();
        services.AddScoped<IEvaluacionService, EvaluacionService>();
        services.AddScoped<IHabilidadService, HabilidadService>();
        services.AddScoped<IMatchingService, MatchingService>();

        return services;
    }
}

