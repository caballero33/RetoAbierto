using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Infrastructure.Data;
using RetoAbierto.Infrastructure.Services;

namespace RetoAbierto.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgresConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection")
            ?? "Host=localhost;Port=5432;Database=RetoAbiertoDB;Username=admin;Password=RetoAbiertoSecret123!";

        services.AddHttpContextAccessor();

        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddDbContext<RetoAbiertoDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<RetoAbiertoDbContext>());

        return services;
    }
}

