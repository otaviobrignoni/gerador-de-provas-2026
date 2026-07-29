using FizzWare.NBuilder;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

namespace GeradorDeProvas.Testes.Integracao.ModuloDisciplina;

[TestClass]
public sealed class RepositorioDisciplinaTests : RepositorioBaseTests
{
    [TestMethod]
    public void CadastrarESelecionarPorId_CarregaRegistro()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Build();

        repositorioDisciplina.Cadastrar(disciplina);
        dbContext.ChangeTracker.Clear();

        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsNotNull(disciplinaSelecionada);
        Assert.AreEqual(disciplina.Nome, disciplinaSelecionada.Nome);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        string novoNome = "NomeAtualizado";
        var disciplinaAtualizada = Builder<Disciplina>
            .CreateNew()
            .With(d => d.Nome = novoNome)
            .With(d => d.UserId = Guid.Empty)
            .Build();

        bool conseguiuEditar = repositorioDisciplina.Editar(disciplina.Id, disciplinaAtualizada);
        dbContext.ChangeTracker.Clear();
        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(disciplinaSelecionada);
        Assert.AreEqual(novoNome, disciplinaSelecionada.Nome);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();

        bool conseguiuExcluir = repositorioDisciplina.Excluir(disciplina.Id);
        dbContext.ChangeTracker.Clear();
        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(disciplinaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_CarregaRegistros()
    {
        var disciplina = Builder<Disciplina>
            .CreateListOfSize(3)
            .All()
            .With(d => d.UserId = Guid.Empty)
            .Persist();

        dbContext.ChangeTracker.Clear();

        var disciplinas = repositorioDisciplina.SelecionarTodos();

        Assert.HasCount(3, disciplinas);
    }
}
