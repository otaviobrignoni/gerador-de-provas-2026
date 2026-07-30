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
        // Arrange
        var (_, materia, questao) = CriarQuestao(1, false);

        // Act
        repositorioQuestao.Cadastrar(questao);
        dbContext.ChangeTracker.Clear();

        var questaoSelecionada = repositorioQuestao.SelecionarPorId(questao.Id);

        // Assert
        Assert.IsNotNull(questaoSelecionada);
        Assert.AreEqual(questao.Enunciado, questaoSelecionada.Enunciado);
        Assert.AreEqual(materia.Id, questaoSelecionada.Materia.Id);
        Assert.HasCount(2, questaoSelecionada.Alternativas);
    }

    [TestMethod]
    public void Editar_AtualizaRegistroExistente()
    {
        // Arrange
        var (_, _, questao) = CriarQuestao(1);
        var (_, materiaAtualizada, questaoAtualizada) = CriarQuestao(2, false);

        // Act
        bool conseguiuEditar = repositorioQuestao.Editar(questao.Id, questaoAtualizada);
        dbContext.ChangeTracker.Clear();

        var questaoSelecionada = repositorioQuestao.SelecionarPorId(questao.Id);

        // Assert
        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(questaoSelecionada);
        Assert.AreEqual(questaoAtualizada.Enunciado, questaoSelecionada.Enunciado);
        Assert.AreEqual(materiaAtualizada.Id, questaoSelecionada.Materia.Id);
        Assert.HasCount(2, questaoSelecionada.Alternativas);
    }

    [TestMethod]
    public void Excluir_RemoveRegistroExistente()
    {
        // Arrange
        var (_, _, questao) = CriarQuestao(1);
        dbContext.ChangeTracker.Clear();

        // Act
        bool conseguiuExcluir = repositorioQuestao.Excluir(questao.Id);
        dbContext.ChangeTracker.Clear();

        // Assert
        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(repositorioQuestao.SelecionarPorId(questao.Id));
    }

    [TestMethod]
    public void SelecionarTodos_SemFiltro_CarregaRegistros_ComRelacionamentos()
    {
        // Arrange
        var questoes = Enumerable
            .Range(1, 3)
            .Select(i => CriarQuestao(i))
            .ToList();

        dbContext.ChangeTracker.Clear();

        // Act
        var questoesSelecionadas = repositorioQuestao.SelecionarTodos();

        // Assert
        var questoesIds = questoes.Select(c => c.questao.Id).ToList();
        var materiasIds = questoes.Select(c => c.materia.Id).ToList();
        var questoesSelecionadasIds = questoesSelecionadas.Select(q => q.Id).ToList();
        var materiasSelecionadasIds = questoesSelecionadas.Select(q => q.Materia.Id).ToList();

        Assert.HasCount(3, questoesSelecionadas);
        CollectionAssert.AreEquivalent(questoesIds, questoesSelecionadasIds);
        CollectionAssert.AreEquivalent(materiasIds, materiasSelecionadasIds);
        Assert.IsTrue(questoesSelecionadas.All(q => q.Alternativas.Count == 2));
    }

    [TestMethod]
    public void SelecionarTodos_ComFiltro_CarregaRegistroCorrespondente_ComRelacionamentos()
    {
        // Arrange
        CriarQuestao(1);
        var (_, materiaEsperada, questaoEsperada) = CriarQuestao(2);
        CriarQuestao(3);

        dbContext.ChangeTracker.Clear();

        // Act
        var questoesSelecionadas = repositorioQuestao
            .SelecionarTodos(q => q.Id == questaoEsperada.Id);

        // Assert
        Assert.HasCount(1, questoesSelecionadas);
        Assert.AreEqual(questaoEsperada.Id, questoesSelecionadas.Single().Id);
        Assert.AreEqual(materiaEsperada.Id, questoesSelecionadas.Single().Materia.Id);
        Assert.HasCount(2, questoesSelecionadas.Single().Alternativas);
    }

    [TestMethod]
    public void SelecionarTodos_ComFiltroSemCorrespondencias_RetornaListaVazia()
    {
        // Arrange
        CriarQuestao(1);
        CriarQuestao(2);
        CriarQuestao(3);

        dbContext.ChangeTracker.Clear();

        // Act
        var questoesSelecionadas = repositorioQuestao
            .SelecionarTodos(q => q.Enunciado == "Enunciado inexistente");

        // Assert
        Assert.IsEmpty(questoesSelecionadas);
    }

    private (Disciplina disciplina, Materia materia, Questao questao) CriarQuestao(int indice, bool persistirQuestao = true)
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
            .Persist();
        var questao = Builder<Questao>
            .CreateNew()
            .With(q => q.Enunciado = $"Enunciado{indice}")
            .With(q => q.Materia = materia)
            .With(q => q.Alternativas = [
                new Alternativa($"Alternativa{indice}A", false),
                new Alternativa($"Alternativa{indice}B", true)
            ])
            .With(q => q.UserId = Guid.Empty)
            .Build();
        if (persistirQuestao)
            repositorioQuestao.Cadastrar(questao);

        return (disciplina, materia, questao);
    }
}
