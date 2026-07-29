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
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materia = Builder<Materia>
            .CreateNew()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Build();

        repositorioMateria.Cadastrar(materia);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(materia.Nome, materiaSelecionada.Nome);
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
        var disciplinaAtualizada = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        string novoNome = "NomeAtualizado";
        var materiaAtualizada = Builder<Materia>
            .CreateNew()
            .With(m => m.Nome = novoNome)
            .With(m => m.Serie = 2)
            .With(m => m.Disciplina = disciplinaAtualizada)
            .With(m => m.UserId = Guid.Empty)
            .Build();

        bool conseguiuEditar = repositorioMateria.Editar(materia.Id, materiaAtualizada);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(novoNome, materiaSelecionada.Nome);
        Assert.AreEqual(2, materiaSelecionada.Serie);
        Assert.AreEqual(disciplinaAtualizada.Nome, materiaSelecionada.Disciplina.Nome);
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

        bool conseguiuExcluir = repositorioMateria.Excluir(materia.Id);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(materiaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_CarregaRegistros_ComRelacionamentos()
    {
        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        var materias = Builder<Materia>
            .CreateListOfSize(3)
            .All()
            .With(m => m.Disciplina = disciplina)
            .With(m => m.UserId = Guid.Empty)
            .Persist();

        var materiasSelecionadas = repositorioMateria.SelecionarTodos();

        Assert.HasCount(3, materiasSelecionadas);
        CollectionAssert.AreEquivalent(materias.ToList(), materiasSelecionadas);
    }
}
