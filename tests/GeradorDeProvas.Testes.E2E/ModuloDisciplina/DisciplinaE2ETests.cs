using GeradorDeProvas.Testes.E2E.Compartilhado;
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

[TestClass]
public sealed class DisciplinaE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemDisciplinas()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.listagem@teste.local", "Senha123!");

        // Act
        await Page.GotoAsync($"{Url}/Disciplina/Listar");

        // Assert
        Assert.AreEqual("/Disciplina/Listar", new Uri(Page.Url).AbsolutePath);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Disciplinas" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nenhuma disciplina cadastrada.", new() { Exact = true })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveCadastrar_Disciplina_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.cadastro@teste.local", "Senha123!");
        await Page.GotoAsync($"{Url}/Disciplina/Listar");

        // Act
        await Page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" }).ClickAsync();
        await Page.GetByLabel("Nome").FillAsync("Matemática");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar" }).ClickAsync();

        // Assert
        Assert.AreEqual("/Disciplina/Listar", new Uri(Page.Url).AbsolutePath);
        await Expect(Page.GetByText("Matemática", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Nenhuma disciplina cadastrada.", new() { Exact = true })).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveEditar_Disciplina_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.edicao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        ILocator card = Page.Locator(".card").Filter(new() { HasText = "Matemática" });
        await card.GetByRole(AriaRole.Link, new() { Name = "Editar", Exact = true }).ClickAsync();

        // Act
        await Page.GetByLabel("Nome").FillAsync("História do Brasil");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar" }).ClickAsync();

        // Assert
        Assert.AreEqual("/Disciplina/Listar", new Uri(Page.Url).AbsolutePath);
        await Expect(Page.GetByText("História do Brasil", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Matemática", new() { Exact = true })).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveExcluir_Disciplina_SemVinculos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.exlusao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        ILocator card = Page.Locator(".card").Filter(new() { HasText = "Matemática" });
        await card.GetByRole(AriaRole.Link, new() { Name = "Excluir", Exact = true }).ClickAsync();

        // Act
        await Expect(Page.GetByText("Deseja realmente excluir esta disciplina?", new() { Exact = true })).ToBeVisibleAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar" }).ClickAsync();

        // Assert
        Assert.AreEqual("/Disciplina/Listar", new Uri(Page.Url).AbsolutePath);
        await Expect(Page.GetByText("Matemática", new() { Exact = true })).Not.ToBeVisibleAsync();
        await Expect(Page.GetByText("Nenhuma disciplina cadastrada.", new() { Exact = true })).ToBeVisibleAsync();
    }

    private async Task CadastrarDisciplinaAsync(string nome)
    {
        await Page.GotoAsync($"{Url}/Disciplina/Cadastrar");

        await Page.GetByLabel("Nome").FillAsync(nome);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Confirmar" }).ClickAsync();

        await Expect(Page.GetByText(nome, new() { Exact = true })).ToBeVisibleAsync();
    }
}
