using FizzWare.NBuilder;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

namespace GeradorDeProvas.Testes.Integracao.ModuloProva;

[TestClass]
public sealed class RepositorioProvaTests : RepositorioBaseTests
{
    [TestMethod]
    public void CadastrarESelecionarPorId_CarregaRelacionamentosDaProva()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Serie = 8)
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questoes = Builder<Questao>
            .CreateListOfSize(5)
            .All()
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Persist();
        var prova = Builder<Prova>
            .CreateNew()
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = materia)
            .With(p => p.Serie = 8)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.Questoes = questoes.ToList())
            .With(p => p.UserId = Guid.Empty)
            .Build();

        repositorioProva.Cadastrar(prova);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        Assert.IsNotNull(provaSelecionada);
        Assert.AreEqual(prova.Titulo, provaSelecionada.Titulo);
        Assert.AreEqual(disciplina.Id, provaSelecionada.Disciplina.Id);
        Assert.AreEqual(materia.Id, provaSelecionada.Materia!.Id);
        Assert.HasCount(5, provaSelecionada.Questoes);
        Assert.IsTrue(provaSelecionada.Questoes.All(q => q.Alternativas.Count == 2));
    }

    [TestMethod]
    public void Editar_AtualizaProvaExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Serie = 8)
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var prova = Builder<Prova>
            .CreateNew()
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = materia)
            .With(p => p.Serie = 8)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.UserId = Guid.Empty)
            .Persist();
        string novoTitulo = "Prova Final";
        var provaAtualizada = Builder<Prova>
            .CreateNew()
            .With(p => p.Titulo = novoTitulo)
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = null)
            .With(p => p.Serie = 8)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.ProvaRecuperacao = true)
            .With(p => p.UserId = Guid.Empty)
            .Build();

        bool conseguiuEditar = repositorioProva.Editar(prova.Id, provaAtualizada);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(provaSelecionada);
        Assert.AreEqual(novoTitulo, provaSelecionada.Titulo);
        Assert.IsTrue(provaSelecionada.ProvaRecuperacao);
        Assert.IsNull(provaSelecionada.Materia);
    }

    [TestMethod]
    public void Excluir_RemoveProvaExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Serie = 8)
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var prova = Builder<Prova>
            .CreateNew()
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = materia)
            .With(p => p.Serie = 8)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.UserId = Guid.Empty)
            .Persist();

        bool conseguiuExcluir = repositorioProva.Excluir(prova.Id);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(provaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_RetornaProvasComRelacionamentos()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.Nome = "Matemática")
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Nome = "Álgebra")
            .With(m => m.Serie = 8)
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();
        var questoes = Builder<Questao>
            .CreateListOfSize(5)
            .All()
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = CriarAlternativas())
            .With(q => q.UserId = Guid.Empty)
            .Persist();
        Builder<Prova>
            .CreateNew()
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = materia)
            .With(p => p.Serie = 8)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.Questoes = questoes.ToList())
            .With(p => p.UserId = Guid.Empty)
            .Persist();

        dbContext.ChangeTracker.Clear();
        var provas = repositorioProva.SelecionarTodos();

        Assert.HasCount(1, provas);
        Assert.AreEqual("Matemática", provas.First().Disciplina.Nome);
        Assert.AreEqual("Álgebra", provas.First().Materia!.Nome);
        Assert.HasCount(5, provas.First().Questoes);
        Assert.IsTrue(provas.First().Questoes.All(q => q.Alternativas.Count == 2));
    }

    private static List<Alternativa> CriarAlternativas() =>
        [new Alternativa("Alternativa1", false), new Alternativa("Alternativa2", true)];
}
