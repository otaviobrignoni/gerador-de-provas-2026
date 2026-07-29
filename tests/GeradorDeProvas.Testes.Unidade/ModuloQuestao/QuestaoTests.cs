using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Testes.Unidade.ModuloQuestao;

[TestClass]
public sealed class QuestaoTests
{
    [TestMethod]
    public void Construtor_DeveVincular_CadaAlternativa_AQuestao()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));
        List<Alternativa> alternativas = [
            new Alternativa("3", false),
            new Alternativa("4", true)
        ];

        // Act
        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        // Assert
        bool alternativasVinculadas = questao.Alternativas.All(a => ReferenceEquals(questao, a.Questao));

        Assert.IsTrue(alternativasVinculadas);
    }

    [TestMethod]
    public void Validar_SemEnunciado_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));
        List<Alternativa> alternativas = [
            new Alternativa("3", false),
            new Alternativa("4", true)
        ];

        var questao = new Questao(string.Empty, materia, alternativas);

        // Act
        var erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Enunciado\" deve ser preenchido e conter no máximo 2000 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_SemMateria_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        List<Alternativa> alternativas = [new Alternativa("3", false), new Alternativa("4", true),];
        var questao = new Questao("Quanto é 2 + 2?", null!, alternativas);

        // Act
        List<string> erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Matéria\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_SemAlternativas_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));

        var questao = new Questao("Quanto é 2 + 2?", materia, []);

        List<string> errosEsperados = [
            "A questão deve possuir no mínimo duas alternativas.",
            "A questão deve possuir uma alternativa correta."
        ];

        // Act
        var erros = questao.Validar();

        // Assert
        Assert.HasCount(2, erros);
        CollectionAssert.AreEqual(errosEsperados, erros);
    }

    [TestMethod]
    public void Validar_ComPoucasAlternativas_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));

        List<Alternativa> alternativas = [
            new Alternativa("4", true)
        ];

        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        // Act
        List<string> erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A questão deve possuir no mínimo duas alternativas.", erros.First());
    }

    [TestMethod]
    public void Validar_ComMuitasAlternativas_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));

        List<Alternativa> alternativas = [
            new Alternativa("4", true),
            new Alternativa("12", false),
            new Alternativa("5", false),
            new Alternativa("7", false),
            new Alternativa("18", false)
        ];

        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        // Act
        var erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A questão deve possuir no máximo quatro alternativas.", erros.First());
    }

    [TestMethod]
    public void Validar_SemAlternativaCorreta_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));

        List<Alternativa> alternativas = [
            new Alternativa("2", false),
            new Alternativa("12", false),
            new Alternativa("5", false),
            new Alternativa("7", false)
        ];

        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        // Act
        var erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A questão deve possuir uma alternativa correta.", erros.First());
    }

    [TestMethod]
    public void Validar_ComMuitasAlternativasCorretas_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));

        List<Alternativa> alternativas = [
            new Alternativa("4", true),
            new Alternativa("12", false),
            new Alternativa("5", false),
            new Alternativa("4", true)
        ];

        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        // Act
        var erros = questao.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A questão deve possuir apenas uma alternativa correta.", erros.First());
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_EnunciadoMateriaEAlternativas()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));
        List<Alternativa> alternativas = [
            new Alternativa("3", false),
            new Alternativa("4", true)
        ];
        var questao = new Questao("Quanto é 2 + 2?", materia, alternativas);

        var materiaAtualizada = new Materia("Geometria", 9, new Disciplina("Matemática"));
        List<Alternativa> alternativasAtualizadas = [
            new Alternativa("25 cm²", true),
            new Alternativa("20 cm²", false)
        ];
        string novoEnunciado = "Qual é a área de um quadrado com lado de 5 cm?";
        var questaoAtualizada = new Questao(novoEnunciado, materiaAtualizada, alternativasAtualizadas);

        // Act
        questao.Atualizar(questaoAtualizada);

        // Assert
        Assert.AreEqual(novoEnunciado, questao.Enunciado);
        Assert.AreSame(materiaAtualizada, questao.Materia);
        CollectionAssert.AreEqual(alternativasAtualizadas, questao.Alternativas);
        Assert.IsTrue(questao.Alternativas.All(a => ReferenceEquals(questao, a.Questao)));
    }
}
