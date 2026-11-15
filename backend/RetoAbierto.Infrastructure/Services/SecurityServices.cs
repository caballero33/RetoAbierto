using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RetoAbierto.Application.Common.Interfaces;
using RetoAbierto.Domain.Entities;

namespace RetoAbierto.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public bool Verify(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Usuario usuario, string rol, Guid? empresaId = null, Guid? universidadId = null, Guid? estudianteId = null)
    {
        var secret = _configuration["JwtSettings:Secret"] ?? "RetoAbiertoSuperSecretSigningKeyForEducationalPurposes2026!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "RetoAbiertoAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "RetoAbiertoClient";
        var expiryHours = int.TryParse(_configuration["JwtSettings:ExpiryHours"], out var hours) ? hours : 8;

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, usuario.Nombre),
            new(ClaimTypes.Name, usuario.Nombre),
            new(JwtRegisteredClaimNames.Email, usuario.Correo),
            new("rol", rol),
            new(ClaimTypes.Role, rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (empresaId.HasValue)
            claims.Add(new Claim("empresaId", empresaId.Value.ToString()));

        if (universidadId.HasValue)
            claims.Add(new Claim("universidadId", universidadId.Value.ToString()));

        if (estudianteId.HasValue)
            claims.Add(new Claim("estudianteId", estudianteId.Value.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

