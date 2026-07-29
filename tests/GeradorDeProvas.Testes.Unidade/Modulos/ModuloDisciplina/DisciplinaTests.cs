using GeradorDeProvas.Dominio.Modulos.ModuloDisciplina;

namespace GeradorDeProvas.Testes.Unidade.Modulos.ModuloDisciplina;

[TestClass]
public sealed class DisciplinaTests
{
    #region Testes da Validação de Disciplina

    [TestMethod]
    public void Validar_ComNomeVazio_DeveRetornarErro()
    {
        var disciplina = new Disciplina(string.Empty);

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeCurto_DeveRetornarErro()
    {
        var disciplina = new Disciplina(new string('A', 1));

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no mínimo 2 caracteres.", erros.First());
    }

    [TestMethod]
    public void Validar_ComNomeLongo_DeveRetornarErro()
    {
        var disciplina = new Disciplina(new string('A', 101));

        var erros = disciplina.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Nome\" deve conter no máximo 100 caracteres.", erros.First());
    }
    #endregion
}
