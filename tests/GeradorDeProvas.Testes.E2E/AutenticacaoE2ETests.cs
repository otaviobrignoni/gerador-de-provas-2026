using GeradorDeProvas.Testes.E2E.Compartilhado;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E;

[TestClass]
public sealed class AutenticacaoE2ETests : PageTest
{
    private TestApplicationFactory aplicacao = null!;
    private string Url { get; set; } = string.Empty;

    [TestInitialize]
    public async Task InicializarAplicacao()
    {
        aplicacao = new TestApplicationFactory();

        Url = aplicacao.Url;
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
            aplicacao = null!;
        }
    }

    [TestMethod]
    public async Task Deve_Exibir_TelaDeLogin_ParaUsuarioAnonimo()
    {
        // Act
        await Page.GotoAsync($"{Url}/");

        // Assert
        await Expect(Page).ToHaveTitleAsync(new Regex("Entrar"));
    }

    [TestMethod]
    public async Task Deve_RegistrarEAutenticar_Usuario()
    {
        // Arrange
        const string email = "novo.usuario@teste.local";
        const string senha = "Senha123!";

        await Page.GotoAsync($"{Url}/Autenticacao/Registrar");

        // Act
        await Page.GetByLabel("E-mail").FillAsync(email);
        await Page.GetByLabel("Senha", new() { Exact = true }).FillAsync(senha);
        await Page.GetByLabel("Confirmar Senha").FillAsync(senha);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Criar Conta" }).ClickAsync();

        // Assert
        string rotaAbsoluta = new Uri(Page.Url).AbsolutePath;

        Assert.AreEqual("/", rotaAbsoluta);
    }
    [TestMethod]
    public async Task Deve_EntrarEAutenticar_Usuario_Valido()
    {
        // Arrange
        const string email = "login.valido@teste.local";
        const string senha = "Senha123!";

        await RegistrarEAutenticarUsuario(email, senha);

        // Act
        await Page.GotoAsync($"{Url}/Autenticacao/Entrar");
        await Page.GetByLabel("E-mail").FillAsync(email);
        await Page.GetByLabel("Senha", new() { Exact = true }).FillAsync(senha);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Entrar" }).ClickAsync();

        // Assert
        string rotaAbsoluta = new Uri(Page.Url).AbsolutePath;

        Assert.AreEqual("/", rotaAbsoluta);

        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = email }))
            .ToBeVisibleAsync();
    }
    private async Task RegistrarEAutenticarUsuario(string email, string senha)
    {
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
}
