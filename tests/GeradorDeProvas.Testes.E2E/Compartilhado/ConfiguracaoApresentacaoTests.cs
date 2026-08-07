using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
public sealed class ConfiguracaoApresentacaoTests
{
    [TestMethod]
    public void Aplicacao_RegistraValidacaoAntiforgeryGlobal()
    {
        using var factory = new TestApplicationFactory();
        MvcOptions options = factory.Services.GetRequiredService<IOptions<MvcOptions>>().Value;

        Assert.Contains(filter => filter is AutoValidateAntiforgeryTokenAttribute, options.Filters);
    }
}
