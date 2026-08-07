using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloDisciplina;
using GeradorDeProvas.Testes.E2E.ModuloQuestao;

namespace GeradorDeProvas.Testes.E2E.ModuloMateria;

[TestClass]
public sealed class MateriaE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemMaterias()
    {
        // Arrange
        await RegistrarEEntrarAsync("materia.listagem@teste.local", "Senha123!");
        MateriaListarPage listarPage = new(Page, Url);

        // Act
        await listarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.Titulo).ToBeVisibleAsync();
        await Expect(listarPage.CadastarNova).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveCadastrar_Materia_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("materia.listagem@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        MateriaFormPage formPage = new(Page, Url);
        MateriaListarPage listarPage = new(Page, Url);

        // Act
        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync("Álgebra", "Matemática", 7);
        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaMateria("Álgebra")).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveEditar_Materia_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("materia.listagem@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);

        MateriaFormPage formPage = new(Page, Url);
        MateriaListarPage listarPage = new(Page, Url);

        await listarPage.EditarAsync("Álgebra");

        // Act
        await formPage.PreencherAsync("Álgebra Linear", "Matemática", 8);
        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaMateria("Álgebra Linear")).ToBeVisibleAsync();
        await Expect(listarPage.NomeDaMateria("Álgebra")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveExcluir_Materia_SemVinculos()
    {
        // Arrange
        await RegistrarEEntrarAsync("materia.listagem@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);

        MateriaListarPage listarPage = new(Page, Url);
        MateriaExcluirPage excluirPage = new(Page);

        await listarPage.ExcluirAsync("Álgebra");

        // Act
        await Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(Url)}/Materia/Excluir/.*"));

        await Expect(excluirPage.MensagemConfirmacao).ToBeVisibleAsync();

        await excluirPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaMateria("Álgebra")).Not.ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task NaoDeveCadastrar_MateriaDuplicada_NemExcluir_MateriaComQuestao()
    {
        // Arrange
        const string mensagemDuplicidade = "Já existe uma matéria com este nome.";
        const string mensagemVinculo = "Não é possível excluir esta matéria, pois ela possui questões vinculadas.";
        await RegistrarEEntrarAsync("materia.restricoes@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastarMateriaAsync("Álgebra", "Matemática", 7);

        MateriaFormPage formPage = new(Page, Url);
        MateriaListarPage listarPage = new(Page, Url);
        MateriaExcluirPage excluirPage = new(Page);

        // Act e Assert - duplicidade normalizada
        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync("  álgebra  ", "Matemática", 8);
        await formPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(formPage.UrlCadastrar);
        await Expect(formPage.MensagemErro(mensagemDuplicidade)).ToBeVisibleAsync();

        // Act e Assert - exclusão vinculada
        QuestaoFormPage questaoFormPage = new(Page, Url);
        await questaoFormPage.IrParaCadastroAsync();
        await questaoFormPage.PreencherAsync("Quanto é 2 + 2?", "Álgebra", ["4", "5"], indiceCorreta: 0);
        await questaoFormPage.ConfirmarAsync();

        await listarPage.IrParaAsync();
        await listarPage.ExcluirAsync("Álgebra");
        await excluirPage.ConfirmarAsync();

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.MensagemErro(mensagemVinculo)).ToBeVisibleAsync();
        await Expect(listarPage.NomeDaMateria("Álgebra")).ToBeVisibleAsync();
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
