using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloDisciplina;

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
