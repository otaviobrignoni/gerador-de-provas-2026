using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Infra.ModuloProva;
using GeradorDeProvas.Testes.Integracao.Identity;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.ModuloProva;

[TestClass]
public sealed class RepositorioProvaTests
{
    private GeradorDeProvasDbContext dbContext = null!;
    private RepositorioProva repositorio = null!;

    [TestInitialize]
    public void InicializarRepositorio()
    {
        var opt = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase("GeradorDeProvasTestDB_Memory")
            .Options;
        dbContext = new GeradorDeProvasDbContext(opt, new FalsoProvedorDeUsuario(Guid.NewGuid()));
        repositorio = new RepositorioProva(dbContext);
    }

    [TestMethod]
    public void CadastrarESelecionarPorId_CarregaRelacionamentosDaProva()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        string titulo = "Prova de Álgebra";
        var prova = new Prova(titulo, disciplina, materia, 8, 5, false);
        List<Questao> questoesDisponiveis = [.. Enumerable
            .Range(1, 5)
            .Select(indice =>
                new Questao(
                    $"Questão {indice}",
                    materia,
                    [new Alternativa("4", false), new Alternativa("7", true)]
                )
            )
        ];
        prova.SortearQuestoes(questoesDisponiveis, 1);

        repositorio.Cadastrar(prova);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorio.SelecionarPorId(prova.Id);

        Assert.IsNotNull(provaSelecionada);
        Assert.AreEqual(titulo, provaSelecionada.Titulo);
        Assert.AreEqual(disciplina.Id, provaSelecionada.Disciplina.Id);
        Assert.AreEqual(materia.Id, provaSelecionada.Materia!.Id);
        Assert.HasCount(5, provaSelecionada.Questoes);
        Assert.IsTrue(provaSelecionada.Questoes.All(q => q.Alternativas.Count == 2));
    }

    [TestMethod]
    public void Editar_AtualizaProvaExistente()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 5, false);
        List<Questao> questoesDisponiveis = [.. Enumerable
            .Range(1, 5)
            .Select(indice =>
                new Questao(
                    $"Questão {indice}",
                    materia,
                    [new Alternativa("4", false), new Alternativa("7", true)]
                )
            )
        ];
        prova.SortearQuestoes(questoesDisponiveis, 1);
        repositorio.Cadastrar(prova);
        string novoTitulo = "Prova Final";
        var provaAtualizada = new Prova(novoTitulo, disciplina, null!, 8, 5, true);

        bool conseguiuEditar = repositorio.Editar(prova.Id, provaAtualizada);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorio.SelecionarPorId(prova.Id) ?? throw new KeyNotFoundException();

        Assert.IsTrue(conseguiuEditar);
        Assert.AreEqual(novoTitulo, provaSelecionada.Titulo);
    }

    [TestMethod]
    public void Excluir_RemoveProvaExistente()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 5, false);
        List<Questao> questoesDisponiveis = [.. Enumerable
            .Range(1, 5)
            .Select(indice =>
                new Questao(
                    $"Questão {indice}",
                    materia,
                    [new Alternativa("4", false), new Alternativa("7", true)]
                )
            )
        ];
        prova.SortearQuestoes(questoesDisponiveis, 1);
        repositorio.Cadastrar(prova);

        bool conseguiuExcluir = repositorio.Excluir(prova.Id);
        dbContext.ChangeTracker.Clear();
        var provaSelecionada = repositorio.SelecionarPorId(prova.Id);

        Assert.IsTrue(conseguiuExcluir);
        Assert.IsNull(provaSelecionada);
    }

    [TestMethod]
    public void SelecionarTodos_RetornaProvasComRelacionamentos()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 5, false);
        List<Questao> questoesDisponiveis = [.. Enumerable
            .Range(1, 5)
            .Select(indice =>
                new Questao(
                    $"Questão {indice}",
                    materia,
                    [new Alternativa("4", false), new Alternativa("7", true)]
                )
            )
        ];
        prova.SortearQuestoes(questoesDisponiveis, 1);
        repositorio.Cadastrar(prova);

        dbContext.ChangeTracker.Clear();

        var provas = repositorio.SelecionarTodos();

        Assert.HasCount(1, provas);
        Assert.AreEqual("Matemática", provas.First().Disciplina.Nome);
        Assert.AreEqual("Álgebra", provas.First().Materia!.Nome);
        Assert.HasCount(5, provas.First().Questoes);
        Assert.IsTrue(provas.First().Questoes.All(q => q.Alternativas.Count == 2));
    }
}
