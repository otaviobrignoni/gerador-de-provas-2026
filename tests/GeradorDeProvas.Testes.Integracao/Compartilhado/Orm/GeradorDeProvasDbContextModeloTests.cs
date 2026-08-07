using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

[TestClass]
public sealed class GeradorDeProvasDbContextModeloTests
{
    [TestMethod]
    public void Modelo_ConfiguraIndicesUnicosPorUsuario()
    {
        using var dbContext = CriarContexto();

        AssertIndiceUnico<Disciplina>(dbContext.Model, nameof(Disciplina.UserId), nameof(Disciplina.Nome));
        AssertIndiceUnico<Materia>(dbContext.Model, nameof(Materia.UserId), nameof(Materia.Nome));
        AssertIndiceUnico<Prova>(dbContext.Model, nameof(Prova.UserId), nameof(Prova.Titulo));
    }

    [TestMethod]
    public void Modelo_ConfiguraComprimentosMaximos()
    {
        using var dbContext = CriarContexto();

        AssertComprimentoMaximo<Disciplina>(dbContext.Model, nameof(Disciplina.Nome), 100);
        AssertComprimentoMaximo<Materia>(dbContext.Model, nameof(Materia.Nome), 100);
        AssertComprimentoMaximo<Questao>(dbContext.Model, nameof(Questao.Enunciado), 2000);
        AssertComprimentoMaximo<Alternativa>(dbContext.Model, nameof(Alternativa.Texto), 1000);
        AssertComprimentoMaximo<Prova>(dbContext.Model, nameof(Prova.Titulo), 100);
    }

    [TestMethod]
    public void Modelo_ConfiguraDeleteBehaviorDasRelacoes()
    {
        using var dbContext = CriarContexto();

        AssertDeleteBehavior<Materia, Disciplina>(dbContext.Model, "DisciplinaId", DeleteBehavior.Restrict);
        AssertDeleteBehavior<Questao, Materia>(dbContext.Model, "MateriaId", DeleteBehavior.Restrict);
        AssertDeleteBehavior<Alternativa, Questao>(dbContext.Model, "QuestaoId", DeleteBehavior.Cascade);
        AssertDeleteBehavior<Prova, Disciplina>(dbContext.Model, "DisciplinaId", DeleteBehavior.Restrict);
        AssertDeleteBehavior<Prova, Materia>(dbContext.Model, "MateriaId", DeleteBehavior.Restrict);
    }

    [TestMethod]
    public void Modelo_ConfiguraAssociacaoEntreProvaEQuestao()
    {
        using var dbContext = CriarContexto();
        var modelo = dbContext.Model;
        var associacao = modelo.FindEntityType("TBProvaQuestao");

        Assert.IsNotNull(associacao);
        Assert.AreEqual(typeof(Dictionary<string, object>), associacao.ClrType);
        Assert.AreEqual("TBProvaQuestao", associacao.GetTableName());

        var chavePrimaria = associacao.FindPrimaryKey();
        Assert.IsNotNull(chavePrimaria);
        CollectionAssert.AreEqual(
            new[] { "ProvasId", "QuestoesId" },
            chavePrimaria.Properties.Select(p => p.Name).ToArray()
        );

        AssertDeleteBehavior(associacao, typeof(Prova), "ProvasId", DeleteBehavior.Cascade);
        AssertDeleteBehavior(associacao, typeof(Questao), "QuestoesId", DeleteBehavior.Cascade);

        var navegacaoProva = modelo.FindEntityType(typeof(Prova))!
            .FindSkipNavigation(nameof(Prova.Questoes));
        var navegacaoQuestao = modelo.FindEntityType(typeof(Questao))!
            .FindSkipNavigation(nameof(Questao.Provas));

        Assert.IsNotNull(navegacaoProva);
        Assert.IsNotNull(navegacaoQuestao);
        Assert.AreSame(associacao, navegacaoProva.JoinEntityType);
        Assert.AreSame(associacao, navegacaoQuestao.JoinEntityType);
        Assert.AreSame(navegacaoQuestao, navegacaoProva.Inverse);
    }

    private static GeradorDeProvasDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase($"modelo-{Guid.CreateVersion7():N}")
            .Options;

        return new GeradorDeProvasDbContext(options);
    }

    private static void AssertIndiceUnico<T>(IModel modelo, params string[] propriedades)
    {
        var tipoEntidade = modelo.FindEntityType(typeof(T));
        Assert.IsNotNull(tipoEntidade);

        var indice = tipoEntidade.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual(propriedades));

        Assert.IsNotNull(indice);
        Assert.IsTrue(indice.IsUnique);
    }

    private static void AssertComprimentoMaximo<T>(IModel modelo, string propriedade, int esperado)
    {
        var tipoEntidade = modelo.FindEntityType(typeof(T));
        Assert.IsNotNull(tipoEntidade);

        var metadadoDaPropriedade = tipoEntidade.FindProperty(propriedade);
        Assert.IsNotNull(metadadoDaPropriedade);
        Assert.AreEqual(esperado, metadadoDaPropriedade.GetMaxLength());
    }

    private static void AssertDeleteBehavior<TDependente, TPrincipal>(
        IModel modelo,
        string propriedade,
        DeleteBehavior esperado
    )
    {
        var tipoDependente = modelo.FindEntityType(typeof(TDependente));
        Assert.IsNotNull(tipoDependente);

        AssertDeleteBehavior(tipoDependente, typeof(TPrincipal), propriedade, esperado);
    }

    private static void AssertDeleteBehavior(
        IEntityType tipoDependente,
        Type tipoPrincipal,
        string propriedade,
        DeleteBehavior esperado
    )
    {
        var chaveEstrangeira = tipoDependente.GetForeignKeys()
            .SingleOrDefault(fk =>
                fk.PrincipalEntityType.ClrType == tipoPrincipal &&
                fk.Properties.Select(p => p.Name).SequenceEqual([propriedade])
            );

        Assert.IsNotNull(chaveEstrangeira);
        Assert.AreEqual(esperado, chaveEstrangeira.DeleteBehavior);
    }
}
