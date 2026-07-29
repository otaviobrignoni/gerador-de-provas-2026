using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Testes.Unidade.ModuloQuestao;

[TestClass]
public sealed class AlternativaTests
{
    [TestMethod]
    public void Validar_SemTexto_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, new Disciplina("Matemática"));
        var alternativa = new Alternativa(string.Empty, false);

        _ = new Questao(
            "Quanto é 2 + 2?",
            materia,
            [alternativa, new Alternativa("4", true)]
        );

        // Act
        List<string> erros = alternativa.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Texto\" da alternativa deve ser preenchido e conter no máximo 1000 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_SemQuestao_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var alternativa = new Alternativa("4", true);

        // Act
        List<string> erros = alternativa.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A alternativa deve estar vinculada a uma questão.", erros.First());
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_TextoECorreta()
    {
        // Arrange
        var alternativa = new Alternativa("3", false);
        var alternativaAtualizada = new Alternativa("4", true);

        // Act
        alternativa.Atualizar(alternativaAtualizada);

        // Assert
        Assert.AreEqual("4", alternativa.Texto);
        Assert.IsTrue(alternativa.Correta);
    }
}
