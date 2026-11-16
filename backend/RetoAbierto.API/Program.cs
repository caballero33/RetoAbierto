using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RetoAbierto.API.Middlewares;
using RetoAbierto.Application;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;
using RetoAbierto.Domain.Enums;
using RetoAbierto.Infrastructure;
using RetoAbierto.Infrastructure.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "RetoAbiertoSuperSecretSigningKeyForEducationalPurposes2026!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "RetoAbiertoAPI";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "RetoAbiertoClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options.Title = "RetoAbierto API Docs";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<RetoAbiertoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        if (db.Database.CanConnect())
        {
            var rolesBase = new[]
            {
                RolesConstantes.AdminPlataforma,
                RolesConstantes.AdminEmpresa,
                RolesConstantes.Evaluador,
                RolesConstantes.AdminUniversidad,
                RolesConstantes.Estudiante
            };

            foreach (var rolNombre in rolesBase)
            {
                if (!db.Roles.Any(r => r.Nombre == rolNombre))
                {
                    db.Roles.Add(new Rol
                    {
                        Nombre = rolNombre,
                        Descripcion = $"Rol predeterminado: {rolNombre}"
                    });
                }
            }

            if (!db.Usuarios.Any(u => u.EsAdminPlataforma))
            {
                db.Usuarios.Add(new Usuario
                {
                    Nombre = "Admin Plataforma",
                    Correo = "admin@retoabierto.com",
                    PasswordHash = hasher.Hash("AdminReto2026!"),
                    EsAdminPlataforma = true,
                    Activo = true,
                    DebeCambiarPassword = false
                });
            }

            var habilidadesIniciales = new[] { "C#", ".NET", "PostgreSQL", "React", "TypeScript", "Docker", "Arquitectura Limpia", "Gestión de Proyectos" };
            foreach (var hab in habilidadesIniciales)
            {
                if (!db.Habilidades.Any(h => h.Nombre == hab))
                {
                    db.Habilidades.Add(new Habilidad { Nombre = hab });
                }
            }

            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Seeding omitido (la base de datos aún no ha sido migrada o conectada).");
    }
}

app.Run();

