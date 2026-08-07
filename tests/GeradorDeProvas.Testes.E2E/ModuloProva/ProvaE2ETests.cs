using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloDisciplina;
using GeradorDeProvas.Testes.E2E.ModuloMateria;
using GeradorDeProvas.Testes.E2E.ModuloQuestao;
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

[TestClass]
public sealed class ProvaE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemProvas()
    {
        // Arrange
        await RegistrarEEntrarAsync("prova.listagem@teste.local", "Senha123!");

        ProvaListarPage listarPage = new(Page, Url);

        // Act
        await listarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.Titulo).ToBeVisibleAsync();
        await Expect(listarPage.GerarNova).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveGerar_Prova_ComDadosValidos()
    {
        // Arrange
        ProvaListarPage listarPage = await GerarProvaAsync("prova.geracao@teste.local", "Prova de Matemática");

        // Act
        await Expect(listarPage.TituloDaProva("Prova de Matemática")).ToBeVisibleAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.EstadoVazio).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveBaixar_PdfE_Gabarito_DeUmaProvaGerada()
    {
        // Arrange
        const string titulo = "Prova de Matemática";

        ProvaListarPage listarPage = await GerarProvaAsync("prova.pdf@teste.local", titulo);

        // Act
        IDownload pdf = await listarPage.BaixarPdfAsync(titulo);
        IDownload gabarito = await listarPage.BaixarGabaritoAsync(titulo);

        // Assert
        Assert.IsNull(await pdf.FailureAsync());
        Assert.IsTrue(pdf.SuggestedFilename.StartsWith("prova-prova-de-matematica-", StringComparison.Ordinal));
        Assert.IsTrue(pdf.SuggestedFilename.EndsWith(".pdf", StringComparison.Ordinal));
        Assert.IsNull(await gabarito.FailureAsync());
        Assert.IsTrue(gabarito.SuggestedFilename.StartsWith("gabarito-prova-de-matematica-", StringComparison.Ordinal));
        Assert.IsTrue(gabarito.SuggestedFilename.EndsWith(".pdf", StringComparison.Ordinal));
    }

    private async Task<ProvaListarPage> GerarProvaAsync(string email, string titulo)
    {
        await RegistrarEEntrarAsync(email, "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastrarMateriaAsync("Álgebra", "Matemática", 7);
        await CadastrarQuestaoAsync("Quanto é 2 + 2?", ["4", "5"]);

        ProvaFormPage formPage = new(Page, Url);
        ProvaSelecionarQuestoesPage selecionarQuestoesPage = new(Page, Url);
        ProvaConfirmarPage confirmarPage = new(Page, Url);
        ProvaListarPage listarPage = new(Page, Url);

        await formPage.IrParaAsync();
        await formPage.PreencherAsync(titulo, "Matemática", 7);
        await formPage.ContinuarAsync();

        await Expect(Page).ToHaveURLAsync(selecionarQuestoesPage.Url);

        await selecionarQuestoesPage.PreencherAsync("Álgebra", 1);
        await selecionarQuestoesPage.SortearAsync();

        await Expect(Page).ToHaveURLAsync(confirmarPage.Url);
        await Expect(confirmarPage.TituloDaProva(titulo)).ToBeVisibleAsync();
        await Expect(confirmarPage.QuestaoSorteada("Quanto é 2 + 2?")).ToBeVisibleAsync();

        await confirmarPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(listarPage.Url);

        return listarPage;
    }

    private async Task CadastrarQuestaoAsync(string enunciado, string[] alternativas)
    {
        QuestaoFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync(enunciado, "Álgebra", alternativas, indiceCorreta: 0);
        await formPage.ConfirmarAsync();

        QuestaoListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }

    private async Task CadastrarMateriaAsync(string nome, string disciplina, int serie)
    {
        MateriaFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync(nome, disciplina, serie);
        await formPage.ConfirmarAsync();

        MateriaListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }

    private async Task CadastrarDisciplinaAsync(string nome)
    {
        DisciplinaFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherNomeAsync(nome);
        await formPage.ConfirmarAsync();

        DisciplinaListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }
}
