using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloDisciplina;
using GeradorDeProvas.Testes.E2E.ModuloMateria;

namespace GeradorDeProvas.Testes.E2E.ModuloQuestao;

[TestClass]
public sealed class QuestaoE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemQuestoes()
    {
        // Arrange
        await RegistrarEEntrarAsync("questao.listagem@teste.local", "Senha123!");

        QuestaoListarPage listarPage = new(Page, Url);

        // Act
        await listarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.Titulo).ToBeVisibleAsync();
        await Expect(listarPage.CadastrarNova).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveCadastrar_Questao_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("questao.cadastro@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);

        QuestaoFormPage formPage = new(Page, Url);
        QuestaoListarPage listarPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();

        // Act
        await formPage.PreencherAsync("Quanto é 2 + 2?", "Álgebra", ["4", "5"], indiceCorreta: 0);

        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.EstadoVazio).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveEditar_Questao_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("questao.edicao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);
        await CadastrarQuestaoAsync("Quanto é 2 + 2?", ["4", "5"], indiceCorreta: 0);

        QuestaoListarPage listarPage = new(Page, Url);
        QuestaoFormPage formPage = new(Page, Url);

        await listarPage.EditarAsync("Quanto é 2 + 2?");

        // Act
        await formPage.PreencherAsync("Quanto é 3 + 3?", "Álgebra", ["6", "7"], indiceCorreta: 0);

        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.EnunciadoDaQuestao("Quanto é 3 + 3?")).ToBeVisibleAsync();
        await Expect(listarPage.EnunciadoDaQuestao("Quanto é 2 + 2?")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveExcluir_Questao_SemVinculos()
    {
        // Arrange
        await RegistrarEEntrarAsync("questao.edicao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);
        await CadastrarQuestaoAsync("Quanto é 2 + 2?", ["4", "5"], indiceCorreta: 0);


        QuestaoListarPage listarPage = new(Page, Url);
        QuestaoExcluirPage excluirPage = new(Page);

        await listarPage.ExcluirAsync("Quanto é 2 + 2?");

        // Act
        await Expect(Page).ToHaveURLAsync(
            new Regex($"{Regex.Escape(Url)}/Questao/Excluir/.*")
        );

        await Expect(excluirPage.MensagemConfirmacao).ToBeVisibleAsync();
        await excluirPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.EnunciadoDaQuestao("Quanto é 2 + 2?"))
            .Not.ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    private async Task CadastrarQuestaoAsync(string enunciado, string[] alternativas, int indiceCorreta)
    {
        QuestaoFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync(enunciado, "Álgebra", alternativas, indiceCorreta);
        await formPage.ConfirmarAsync();

        QuestaoListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }

    private async Task CadastarMateriaAsync(string nome, string disciplina, int serie)
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
