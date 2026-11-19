namespace RetoAbierto.IntegrationTests;

public class TenantValidoTests
{
    [Fact]
    // Antes este archivo contenía una prueba vacía que no comprobaba ningún comportamiento.
    // Ahora verifica el caso permitido: una identidad autenticada puede aportar una empresa válida.
    // Complementa los casos rechazados de AislamientoTests, sin conectarse a una base de datos.
    public void Empresa_valida_se_lee_de_una_identidad_autenticada()
    {
        var empresa = Guid.NewGuid();
        var accessor = new Microsoft.AspNetCore.Http.HttpContextAccessor
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };
        accessor.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                new[] { new System.Security.Claims.Claim("empresaId", empresa.ToString()) }, "prueba"));
        Assert.Equal(empresa, new RetoAbierto.Infrastructure.Services.TenantService(accessor).EmpresaId);
    }
}
