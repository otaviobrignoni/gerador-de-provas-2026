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
        // Arrange
        var (disciplina, materia) = CriarMateria(1, false);

        // Act
        repositorioMateria.Cadastrar(materia);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        // Assert
        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(materia.Nome, materiaSelecionada.Nome);
        Assert.AreEqual(disciplina.Id, materiaSelecionada.Disciplina.Id);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        // Arrange
        var (_, materia) = CriarMateria(1);
        var (disciplinaAtualizada, materiaAtualizada) = CriarMateria(2, false);

        // Act
        bool conseguiuEditar = repositorioMateria.Editar(materia.Id, materiaAtualizada);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        // Assert
        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(materiaSelecionada);
        Assert.AreEqual(materiaAtualizada.Nome, materiaSelecionada.Nome);
        Assert.AreEqual(materiaAtualizada.Serie, materiaSelecionada.Serie);
        Assert.AreEqual(disciplinaAtualizada.Id, materiaSelecionada.Disciplina.Id);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        // Arrange
        var (_, materia) = CriarMateria(1);

        // Act
        bool conseguiuExcluir = repositorioMateria.Excluir(materia.Id);
        dbContext.ChangeTracker.Clear();
        var materiaSelecionada = repositorioMateria.SelecionarPorId(materia.Id);

        // Assert
        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(materiaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_SemFiltro_CarregaRegistros_ComRelacionamentos()
    {
        // Arrange
        var materias = Enumerable
            .Range(1, 3)
            .Select(i => CriarMateria(i).materia)
            .ToList();

        dbContext.ChangeTracker.Clear();

        // Act
        var materiasSelecionadas = repositorioMateria.SelecionarTodos();

        // Assert
        var materiasIds = materias.Select(m => m.Id).ToList();
        var materiasSelecionadasIds = materiasSelecionadas.Select(m => m.Id).ToList();

        Assert.HasCount(3, materiasSelecionadas);
        CollectionAssert.AreEquivalent(materiasIds, materiasSelecionadasIds);
        Assert.IsTrue(materiasSelecionadas.All(m => m.Disciplina is not null));
    }

    [TestMethod]
    public void SelecionarTodos_ComFiltro_CarregaRegistroCorrespondente_ComRelacionamentos()
    {
        // Arrange
        CriarMateria(1);
        var (disciplinaEsperada, materiaEsperada) = CriarMateria(2);
        CriarMateria(3);

        dbContext.ChangeTracker.Clear();

        // Act
        var materiasSelecionadas = repositorioMateria
            .SelecionarTodos(m => m.Serie == materiaEsperada.Serie);

        // Assert
        Assert.HasCount(1, materiasSelecionadas);
        Assert.AreEqual(materiaEsperada.Id, materiasSelecionadas.Single().Id);
        Assert.AreEqual(disciplinaEsperada.Id, materiasSelecionadas.Single().Disciplina.Id);
    }

    [TestMethod]
    public void SelecionarTodos_ComFiltroSemCorrespondencias_RetornaListaVazia()
    {
        // Arrange
        CriarMateria(1);
        CriarMateria(2);
        CriarMateria(3);

        dbContext.ChangeTracker.Clear();

        // Act
        var materiasSelecionadas = repositorioMateria
            .SelecionarTodos(m => m.Serie == 99);

        // Assert
        Assert.IsEmpty(materiasSelecionadas);
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
