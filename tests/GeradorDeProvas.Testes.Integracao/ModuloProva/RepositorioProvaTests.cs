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
        // Arrange
        var (disciplina, materia, questoes, prova) = CriarProva(1, false);

        // Act
        repositorioProva.Cadastrar(prova);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        // Assert
        Assert.IsNotNull(provaSelecionada);
        Assert.AreEqual(prova.Titulo, provaSelecionada.Titulo);
        Assert.AreEqual(disciplina.Id, provaSelecionada.Disciplina.Id);
        Assert.AreEqual(materia!.Id, provaSelecionada.Materia!.Id);
        Assert.HasCount(questoes.Count, provaSelecionada.Questoes);
        Assert.IsTrue(provaSelecionada.Questoes.All(q => q.Alternativas.Count == 2));
    }

    [TestMethod]
    public void Editar_AtualizaProvaExistente()
    {
        // Arrange
        var (_, _, _, prova) = CriarProva(1);
        var (_, _, _, provaAtualizada) = CriarProva(2, false, true);

        // Act
        bool conseguiuEditar = repositorioProva.Editar(prova.Id, provaAtualizada);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        // Assert
        Assert.IsTrue(conseguiuEditar);
        Assert.IsNotNull(provaSelecionada);
        Assert.AreEqual(provaAtualizada.Titulo, provaSelecionada.Titulo);
        Assert.IsTrue(provaSelecionada.ProvaRecuperacao);
        Assert.IsNull(provaSelecionada.Materia);
    }

    [TestMethod]
    public void Excluir_RemoveProvaExistente()
    {
        // Arrange
        var (_, _, _, prova) = CriarProva(1);

        // Act
        bool conseguiuExcluir = repositorioProva.Excluir(prova.Id);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorioProva.SelecionarPorId(prova.Id);

        // Assert
        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(provaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_RetornaProvasComRelacionamentos()
    {
        // Arrange
        var (disciplina, materia, questoes, _) = CriarProva(1);

        dbContext.ChangeTracker.Clear();

        // Act
        var provas = repositorioProva.SelecionarTodos();

        // Assert
        Assert.HasCount(1, provas);
        Assert.AreEqual(disciplina.Nome, provas.First().Disciplina.Nome);
        Assert.AreEqual(materia!.Nome, provas.First().Materia!.Nome);
        Assert.HasCount(questoes.Count, provas.First().Questoes);
        Assert.IsTrue(provas.First().Questoes.All(q => q.Alternativas.Count == 2));
    }

    private (Disciplina disciplina, Materia? materia, List<Questao> questoes, Prova prova) CriarProva(int indice, bool persistirProva = true, bool provaRecuperacao = false)
    {
        if (indice <= 0)
            throw new ArgumentOutOfRangeException(nameof(indice), "O índice deve ser maior que zero.");

        var disciplina = Builder<Disciplina>
            .CreateNew()
            .With(d => d.Nome = $"Disciplina{indice}")
            .With(d => d.UserId = Guid.Empty)
            .Persist();
        Materia? materia = null;
        List<Questao> questoes = [];
        if (!provaRecuperacao)
        {
            materia = Builder<Materia>
                .CreateNew()
                .With(m => m.Nome = $"Materia{indice}")
                .With(m => m.Serie = indice)
                .With(m => m.Disciplina = disciplina)
                .With(m => m.UserId = Guid.Empty)
                .Persist();
            questoes = [.. Builder<Questao>
                .CreateListOfSize(5)
                .All()
                .With(q => q.Materia = materia)
                .With(q => q.Alternativas = [
                    new Alternativa($"Alternativa{indice}A", false),
                    new Alternativa($"Alternativa{indice}B", true)
                ])
                .With(q => q.UserId = Guid.Empty)
                .Persist()
            ];
        }
        var prova = Builder<Prova>
            .CreateNew()
            .With(p => p.Titulo = $"Prova{indice}")
            .With(p => p.Disciplina = disciplina)
            .With(p => p.Materia = materia)
            .With(p => p.Serie = indice)
            .With(p => p.QuantidadeQuestoes = 5)
            .With(p => p.ProvaRecuperacao = provaRecuperacao)
            .With(p => p.Questoes = questoes)
            .With(p => p.UserId = Guid.Empty)
            .Build();
        if (persistirProva)
            repositorioProva.Cadastrar(prova);

        return (disciplina, materia, questoes, prova);
    }
}
