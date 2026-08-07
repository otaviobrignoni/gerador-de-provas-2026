using GeradorDeProvas.Dominio.ModuloDisciplina;

namespace GeradorDeProvas.Testes.Unidade.ModuloDisciplina;

[TestClass]
public sealed class DisciplinaTests
{
    [TestMethod]
    public void Validar_ComNomeVazio_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina(string.Empty);

        // Act
        var erros = disciplina.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeCurto_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina(new string('A', 1));

        // Act
        var erros = disciplina.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no mínimo 2 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeLongo_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina(new string('A', 101));

        // Act
        var erros = disciplina.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no máximo 100 caracteres.", erros.First());
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(100)]
    public void Validar_ComNomeNosLimites_NaoDeveRetornarErros(int quantidadeCaracteres)
    {
        // Arrange
        var disciplina = new Disciplina(new string('A', quantidadeCaracteres));

        // Act
        var erros = disciplina.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_Nome()
    {
        // Arrange
        var disciplina = new Disciplina("nomeOriginal");

        var disciplinaAtualizada = new Disciplina("nomeAtualizado");

        // Act
        disciplina.Atualizar(disciplinaAtualizada);

        // Assert
        Assert.AreEqual("nomeAtualizado", disciplina.Nome);
    }
}
