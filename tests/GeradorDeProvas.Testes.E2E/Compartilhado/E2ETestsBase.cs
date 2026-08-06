using GeradorDeProvas.Testes.E2E.ModuloAutenticacao;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

public abstract class E2ETestsBase : PageTest
{
    private TestApplicationFactory? aplicacao;

    protected string Url => aplicacao?.Url
        ?? throw new InvalidOperationException("A aplicação de teste não foi inicializada.");

    [TestInitialize]
    public async Task InicializarAplicacao()
    {
        aplicacao = new TestApplicationFactory();
    }

    [TestCleanup]
    public async Task EncerrarAplicacao()
    {
        try
        {
            if (aplicacao is not null)
                await aplicacao.DisposeAsync();
        }
        finally
        {
            aplicacao = null;
        }
    }

    protected async Task RegistrarUsuarioAsync(string email, string senha)
    {
        if (aplicacao is null)
            throw new InvalidOperationException("A aplicação de teste não foi inicializada.");

        using IServiceScope scope = aplicacao.Services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();

        var user = new IdentityUser<Guid>()
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email
        };

        IdentityResult resultado = await userManager.CreateAsync(user, senha);

        Assert.IsTrue(resultado.Succeeded, string.Join("; ", resultado.Errors.Select(erro => erro.Description)));
    }

    protected async Task RegistrarEEntrarAsync(string email, string senha)
    {
        await RegistrarUsuarioAsync(email, senha);

        EntrarPage entrarPage = new(Page, Url);

        await entrarPage.IrParaAsync();
        await entrarPage.PreencherAsync(email, senha);
        await entrarPage.ConfirmarAsync();
    }
}
