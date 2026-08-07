using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloMateria;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

[TestClass]
public sealed class DisciplinaE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemDisciplinas()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.listagem@teste.local", "Senha123!");
        DisciplinaListarPage listarPage = new(Page, Url);

        // Act
        await listarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.Titulo).ToBeVisibleAsync();
        await Expect(listarPage.CadastarNova).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveCadastrar_Disciplina_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.cadastro@teste.local", "Senha123!");

        DisciplinaFormPage formPage = new(Page, Url);
        DisciplinaListarPage listarPage = new(Page, Url);

        // Act
        await formPage.IrParaCadastroAsync();
        await formPage.PreencherNomeAsync("Matemática");
        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaDisciplina("Matemática")).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveEditar_Disciplina_ComDadosValidos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.edicao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        DisciplinaFormPage formPage = new(Page, Url);
        DisciplinaListarPage listarPage = new(Page, Url);

        await listarPage.EditarAsync("Matemática");

        // Act
        await formPage.PreencherNomeAsync("História do Brasil");
        await formPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaDisciplina("História do Brasil")).ToBeVisibleAsync();
        await Expect(listarPage.NomeDaDisciplina("Matemática")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveExcluir_Disciplina_SemVinculos()
    {
        // Arrange
        await RegistrarEEntrarAsync("disciplina.exlusao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        DisciplinaListarPage listarPage = new(Page, Url);
        DisciplinaExcluirPage excluirPage = new(Page);

        await listarPage.ExcluirAsync("Matemática");

        // Act
        await Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(Url)}/Disciplina/Excluir/.*"));
        await Expect(excluirPage.MensagemConfirmacao).ToBeVisibleAsync();
        await excluirPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.NomeDaDisciplina("Matemática")).Not.ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task NaoDeveCadastrar_DisciplinaDuplicada_NemExcluir_DisciplinaComMateria()
    {
        // Arrange
        const string mensagemDuplicidade = "Já existe uma disciplina com este nome.";
        const string mensagemVinculo = "Não é possível excluir esta disciplina, pois ela possui matérias vinculadas.";
        await RegistrarEEntrarAsync("disciplina.restricoes@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");

        DisciplinaFormPage formPage = new(Page, Url);
        DisciplinaListarPage listarPage = new(Page, Url);
        DisciplinaExcluirPage excluirPage = new(Page);

        // Act e Assert - duplicidade normalizada
        await formPage.IrParaCadastroAsync();
        await formPage.PreencherNomeAsync("  matemática  ");
        await formPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(formPage.UrlCadastrar);
        await Expect(formPage.MensagemErro(mensagemDuplicidade)).ToBeVisibleAsync();

        // Act e Assert - exclusão vinculada
        MateriaFormPage materiaFormPage = new(Page, Url);
        await materiaFormPage.IrParaCadastroAsync();
        await materiaFormPage.PreencherAsync("Álgebra", "Matemática", 7);
        await materiaFormPage.ConfirmarAsync();

        await listarPage.IrParaAsync();
        await listarPage.ExcluirAsync("Matemática");
        await excluirPage.ConfirmarAsync();

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.MensagemErro(mensagemVinculo)).ToBeVisibleAsync();
        await Expect(listarPage.NomeDaDisciplina("Matemática")).ToBeVisibleAsync();
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
