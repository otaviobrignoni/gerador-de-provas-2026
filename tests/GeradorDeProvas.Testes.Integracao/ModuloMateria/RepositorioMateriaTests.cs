using FizzWare.NBuilder;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

namespace GeradorDeProvas.Testes.Integracao.ModuloMateria;

[TestClass]
public sealed class RepositorioMateriaTests : RepositorioBaseTests
{
    [TestMethod]
    public void CadastrarESelecionarPorId_CarregaRegistro_ComRelacionamentos()
    {
        var (disciplina, materia) = CriarMateria(1, false);

        repositorioMateria.Cadastrar(materia);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(materia.Nome, materiaSelecionada.Nome);
        Assert.AreEqual(disciplina.Id, materiaSelecionada.Disciplina.Id);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        var (_, materia) = CriarMateria(1);
        var (disciplinaAtualizada, materiaAtualizada) = CriarMateria(2, false);

        bool conseguiuEditar = repositorioMateria.Editar(materia.Id, materiaAtualizada);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(materiaAtualizada.Nome, materiaSelecionada.Nome);
        Assert.AreEqual(materiaAtualizada.Serie, materiaSelecionada.Serie);
        Assert.AreEqual(disciplinaAtualizada.Id, materiaSelecionada.Disciplina.Id);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        var (_, materia) = CriarMateria(1);

        bool conseguiuExcluir = repositorioMateria.Excluir(materia.Id);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(materiaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_CarregaRegistros_ComRelacionamentos()
    {
        var materias = Enumerable
            .Range(1, 3)
            .Select(i => CriarMateria(i).materia)
            .ToList();

        dbContext.ChangeTracker.Clear();
        var materiasSelecionadas = repositorioMateria.SelecionarTodos();
        var materiasIds = materias.Select(m => m.Id).ToList();
        var materiasSelecionadasIds = materiasSelecionadas.Select(m => m.Id).ToList();

        Assert.HasCount(3, materiasSelecionadas);
        CollectionAssert.AreEquivalent(materiasIds, materiasSelecionadasIds);
        Assert.IsTrue(materiasSelecionadas.All(m => m.Disciplina is not null));
    }

    private (Disciplina disciplina, Materia materia) CriarMateria(int indice, bool persistirMateria = true)
    {
        if (indice <= 0)
            throw new ArgumentOutOfRangeException(nameof(indice), "O índice deve ser maior que zero.");

        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.Nome = $"Disciplina{indice}")
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Nome = $"Materia{indice}")
            .With(m => m.Serie = indice)
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Build();
        if (persistirMateria)
            repositorioMateria.Cadastrar(materia);

        return (disciplina, materia);
    }
}
