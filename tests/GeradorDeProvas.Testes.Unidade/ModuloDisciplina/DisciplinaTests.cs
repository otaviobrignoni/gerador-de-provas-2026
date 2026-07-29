using GeradorDeProvas.Dominio.ModuloDisciplina;

namespace GeradorDeProvas.Testes.Unidade.ModuloDisciplina;

[TestClass]
public sealed class DisciplinaTests
{
    [TestMethod]
    public void Validar_ComNomeVazio_DeveRetornar_ErroCorrespondente()
    {
        var disciplina = new Disciplina(string.Empty);

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeCurto_DeveRetornar_ErroCorrespondente()
    {
        var disciplina = new Disciplina(new string('A', 1));

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no mínimo 2 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeLongo_DeveRetornar_ErroCorrespondente()
    {
        var disciplina = new Disciplina(new string('A', 101));

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no máximo 100 caracteres.", erros.First());
    }

    [TestMethod]
    public void Atualizar_DeveAtualizar_Nome()
    {
        var disciplina = new Disciplina("nomeOriginal");

        var disciplinaAtualizada = new Disciplina("nomeAtualizado");

        disciplina.Atualizar(disciplinaAtualizada);

        Assert.AreEqual("nomeAtualizado", disciplina.Nome);
    }
}
