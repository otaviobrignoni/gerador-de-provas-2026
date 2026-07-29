using FizzWare.NBuilder;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

namespace GeradorDeProvas.Testes.Integracao.ModuloQuestao;

[TestClass]
public sealed class RepositorioQuestaoTests : RepositorioBaseTests
{
    [TestMethod]
    public void CadastrarESelecionarPorId_CarregaRegistro_ComRelacionamentos()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questao = Builder<Questao>
            .CreateNew()
            .With(q => q.Enunciado = "Enunciado1")
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Build();

        repositorioQuestao.Cadastrar(questao);
        dbContext.ChangeTracker.Clear();

        var questaoSelecionada = repositorioQuestao.SelecionarPorId(questao.Id);

        Assert.IsNotNull(questaoSelecionada);
        Assert.AreEqual("Enunciado1", questaoSelecionada.Enunciado);
        Assert.AreEqual(materia.Id, questaoSelecionada.Materia.Id);
        Assert.HasCount(2, questaoSelecionada.Alternativas);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questao = Builder<Questao>
            .CreateNew()
            .With(q => q.Enunciado = "Enunciado1")
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Persist();
        var disciplinaAtualizada = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materiaAtualizada = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplinaAtualizada)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questaoAtualizada = Builder<Questao>
            .CreateNew()
            .With(q => q.Enunciado = "EnunciadoAtualizado")
            .With(q => q.Materia = materiaAtualizada)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Build();

        bool conseguiuEditar = repositorioQuestao.Editar(questao.Id, questaoAtualizada);
        dbContext.ChangeTracker.Clear();

        var questaoSelecionada = repositorioQuestao.SelecionarPorId(questao.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(questaoSelecionada);
        Assert.AreEqual("EnunciadoAtualizado", questaoSelecionada.Enunciado);
        Assert.AreEqual(materiaAtualizada.Id, questaoSelecionada.Materia.Id);
        Assert.HasCount(2, questaoSelecionada.Alternativas);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questao = Builder<Questao>
            .CreateNew()
            .With(q => q.Enunciado = "Enunciado1")
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Persist();
        dbContext.ChangeTracker.Clear();

        bool conseguiuExcluir = repositorioQuestao.Excluir(questao.Id);
        dbContext.ChangeTracker.Clear();

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(repositorioQuestao.SelecionarPorId(questao.Id));
    }

    [TestMethod]
    public void SelecionarTodos_CarregaRegistros_ComRelacionamentos()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questoes = Builder<Questao>
            .CreateListOfSize(3)
            .All()
            .With(q => q.Enunciado = "Enunciado1")
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Persist();

        dbContext.ChangeTracker.Clear();
        var questoesSelecionadas = repositorioQuestao.SelecionarTodos();

        Assert.HasCount(3, questoesSelecionadas);
        CollectionAssert.AreEquivalent(questoes.Select(q => q.Id).ToList(), questoesSelecionadas.Select(q => q.Id).ToList());
        Assert.IsTrue(questoesSelecionadas.All(q => q.Materia.Id == materia.Id));
        Assert.IsTrue(questoesSelecionadas.All(q => q.Alternativas.Count == 2));
    }

    private static List<Alternativa> CriarAlternativas() =>
        [new Alternativa("Alternativa1", false), new Alternativa("Alternativa2", true)];
}
