using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;

namespace GeradorDeProvas.Testes.Unidade.ModuloMateria;

[TestClass]
public sealed class MateriaTests
{
    [TestMethod]
    public void Validar_SemNome_DeveRetornar_ErroCorrespondente()
    {
        // Arranjo
        var disciplina = new Disciplina("Matemática");

        var materia = new Materia(string.Empty, 5, disciplina);

        // Ação
        var erros = materia.Validar();

        // Asserção
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter entre 2 e 100 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_SemSerie_DeveRetornar_ErroCorrespondente()
    {
        // Arranjo
        var disciplina = new Disciplina("Matemática");

        var materia = new Materia("Quatro Operações", 0, disciplina);

        // Ação
        var erros = materia.Validar();

        // Asserção
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_SemDisciplina_DeveRetornar_ErroCorrespondente()
    {
        // Arranjo
        Disciplina? disciplina = null;

        var materia = new Materia("Quatro Operações", 2, disciplina!);

        // Ação
        var erros = materia.Validar();

        // Asserção
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Disciplina\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_NomeSerieEDisciplina()
    {
        // Arranjo
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Quatro Operações", 2, disciplina);

        var disciplinaAtualizada = new Disciplina("História");
        var materiaAtualizada = new Materia("História do Brasil", 5, disciplinaAtualizada);

        // Ação
        materia.Atualizar(materiaAtualizada);

        // Asserção
        Assert.AreEqual("História do Brasil", materia.Nome);
        Assert.AreEqual(5, materia.Serie);
        Assert.AreSame(disciplinaAtualizada, materia.Disciplina);
    }
}
