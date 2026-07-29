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
        var disciplina = CriarDisciplina(1, false);

        repositorioDisciplina.Cadastrar(disciplina);
        dbContext.ChangeTracker.Clear();

        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsNotNull(disciplinaSelecionada);
        Assert.AreEqual(disciplina.Nome, disciplinaSelecionada.Nome);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        var disciplina = CriarDisciplina(1);
        var disciplinaAtualizada = CriarDisciplina(2, false);

        bool conseguiuEditar = repositorioDisciplina.Editar(disciplina.Id, disciplinaAtualizada);
        dbContext.ChangeTracker.Clear();
        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(disciplinaSelecionada);
        Assert.AreEqual(disciplinaAtualizada.Nome, disciplinaSelecionada.Nome);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        var disciplina = CriarDisciplina(1);

        bool conseguiuExcluir = repositorioDisciplina.Excluir(disciplina.Id);
        dbContext.ChangeTracker.Clear();
        var disciplinaSelecionada = repositorioDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(disciplinaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_CarregaRegistros()
    {
        var disciplinas = Enumerable
            .Range(1, 3)
            .Select(i => CriarDisciplina(i))
            .ToList();

        dbContext.ChangeTracker.Clear();

        var disciplinasSelecionadas = repositorioDisciplina.SelecionarTodos();
        var disciplinasIds = disciplinas.Select(d => d.Id).ToList();
        var disciplinasSelecionadasIds = disciplinasSelecionadas.Select(d => d.Id).ToList();

        Assert.HasCount(3, disciplinasSelecionadas);
        CollectionAssert.AreEquivalent(disciplinasIds, disciplinasSelecionadasIds);
    }

    private Disciplina CriarDisciplina(int indice, bool persistir = true)
    {
        if (indice <= 0)
            throw new ArgumentOutOfRangeException(nameof(indice), "O índice deve ser maior que zero.");

        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.Nome = $"Disciplina{indice}")
            .With(d => d.UserId = Guid.Empty)
            .Build();
        if (persistir)
            repositorioDisciplina.Cadastrar(disciplina);

        return disciplina;
    }
}
