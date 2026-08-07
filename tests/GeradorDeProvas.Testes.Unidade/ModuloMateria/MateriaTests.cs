using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;

namespace GeradorDeProvas.Testes.Unidade.ModuloMateria;

[TestClass]
public sealed class MateriaTests
{
    [TestMethod]
    public void Validar_SemNome_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");

        var materia = new Materia(string.Empty, 5, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter entre 2 e 100 caracteres.", erros.First());
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(101)]
    public void Validar_ComNomeForaDosLimites_DeveRetornar_ErroCorrespondente(int quantidadeCaracteres)
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia(new string('A', quantidadeCaracteres), 5, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter entre 2 e 100 caracteres.", erros.First());
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(100)]
    public void Validar_ComNomeNosLimites_NaoDeveRetornarErros(int quantidadeCaracteres)
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia(new string('A', quantidadeCaracteres), 5, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Validar_SemSerie_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");

        var materia = new Materia("Quatro Operações", 0, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComSerieNegativa_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Quatro Operações", -1, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComDadosValidos_NaoDeveRetornarErros()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Quatro Operações", 1, disciplina);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Validar_SemDisciplina_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        Disciplina? disciplina = null;

        var materia = new Materia("Quatro Operações", 2, disciplina!);

        // Act
        var erros = materia.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Disciplina\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_NomeSerieEDisciplina()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Quatro Operações", 2, disciplina);

        var disciplinaAtualizada = new Disciplina("História");
        var materiaAtualizada = new Materia("História do Brasil", 5, disciplinaAtualizada);

        // Act
        materia.Atualizar(materiaAtualizada);

        // Assert
        Assert.AreEqual("História do Brasil", materia.Nome);
        Assert.AreEqual(5, materia.Serie);
        Assert.AreSame(disciplinaAtualizada, materia.Disciplina);
    }
}
