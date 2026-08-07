using System.Security.Claims;
using GeradorDeProvas.WebApp.Compartilhado.Identity;
using Microsoft.AspNetCore.Http;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
public sealed class ProvedorDeUsuarioTests
{
    [TestMethod]
    public void Id_UsuarioAutenticadoComIdentificadorValido_RetornaIdentificador()
    {
        Guid id = Guid.CreateVersion7();
        ProvedorDeUsuario provedor = CriarProvedor(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "Teste")
        );

        Assert.AreEqual(id, provedor.Id);
        Assert.IsTrue(provedor.EstaAutenticado);
    }

    [TestMethod]
    public void Id_UsuarioNaoAutenticado_RetornaNulo()
    {
        ProvedorDeUsuario provedor = CriarProvedor(new ClaimsIdentity());

        Assert.IsNull(provedor.Id);
        Assert.IsFalse(provedor.EstaAutenticado);
    }

    [TestMethod]
    public void Id_ClaimAusenteOuInvalida_RetornaNulo()
    {
        ProvedorDeUsuario semClaim = CriarProvedor(new ClaimsIdentity([], "Teste"));
        ProvedorDeUsuario claimInvalida = CriarProvedor(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "invalido")], "Teste")
        );

        Assert.IsNull(semClaim.Id);
        Assert.IsNull(claimInvalida.Id);
    }

    private static ProvedorDeUsuario CriarProvedor(ClaimsIdentity identidade)
    {
        var contexto = new DefaultHttpContext { User = new ClaimsPrincipal(identidade) };
        var accessor = new HttpContextAccessor { HttpContext = contexto };

        return new ProvedorDeUsuario(accessor);
    }
}
