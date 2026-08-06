using GeradorDeProvas.Testes.E2E.Compartilhado;

namespace GeradorDeProvas.Testes.E2E.ModuloAutenticacao;

[TestClass]
public sealed class AutenticacaoE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task Deve_Exibir_TelaDeLogin_ParaUsuarioAnonimo()
    {
        // Arrange
        EntrarPage entrarPage = new(Page, Url);

        // Act
        await entrarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(entrarPage.Url);
        await Expect(entrarPage.Titulo).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Deve_RegistrarEAutenticar_Usuario()
    {
        // Arrange
        const string email = "novo.usuario@teste.local";
        const string senha = "Senha123!";

        RegistrarPage registrarPage = new(Page, Url);

        // Act
        await registrarPage.IrParaAsync();
        await registrarPage.PreencherAsync(email, senha);
        await registrarPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync($"{Url}/");
    }

    [TestMethod]
    public async Task Deve_EntrarEAutenticar_Usuario_Valido()
    {
        // Arrange
        const string email = "login.valido@teste.local";
        const string senha = "Senha123!";

        await RegistrarUsuarioAsync(email, senha);

        EntrarPage entrarPage = new(Page, Url);

        // Act
        await entrarPage.IrParaAsync();
        await entrarPage.PreencherAsync(email, senha);
        await entrarPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync($"{Url}/");
        await Expect(entrarPage.UsuarioAutenticado(email)).ToBeVisibleAsync();
    }
}
