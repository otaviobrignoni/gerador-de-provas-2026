using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm.Database;

[TestClass]
[TestCategory("Database")]
public sealed partial class SqlServerDatabaseTests
{
    private static SqlServerDatabaseFixture Fixture { get; } = new();

    [ClassInitialize]
    public static Task InicializarClasse(TestContext _) =>
        Fixture.VerificarServidorDisponivelAsync();

    [ClassCleanup]
    public static async Task LimparClasse() => await Fixture.DisposeAsync();

    [TestInitialize]
    public Task InicializarTeste() => Fixture.CriarBancoLimpoAsync();

    [TestCleanup]
    public Task LimparTeste() => Fixture.ExcluirBancoAtualAsync();

    [TestMethod]
    public async Task Migrate_BancoVazio_AplicaTodasAsMigracoes()
    {
        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContextoSemUsuario();

        string[] migracoesDefinidas = [.. dbContext.Database.GetMigrations()];
        string[] migracoesAplicadas = [.. await dbContext.Database.GetAppliedMigrationsAsync()];
        string[] migracoesPendentes = [.. await dbContext.Database.GetPendingMigrationsAsync()];

        Assert.AreEqual("Microsoft.EntityFrameworkCore.SqlServer", dbContext.Database.ProviderName);
        Assert.IsTrue(await dbContext.Database.CanConnectAsync());
        StringAssert.StartsWith(
            dbContext.Database.GetDbConnection().Database,
            SqlServerDatabaseFixture.PrefixoBanco
        );
        CollectionAssert.AreEqual(migracoesDefinidas, migracoesAplicadas);
        Assert.IsEmpty(migracoesPendentes);
        Assert.HasCount(6, migracoesAplicadas);
    }

    [TestMethod]
    public async Task Salvar_DisciplinasComMesmoNomeParaMesmoUsuario_LancaDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string nome = $"Disciplina {Guid.NewGuid():N}";

        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContexto(usuarioId);
        dbContext.Disciplinas.Add(new Disciplina(nome));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        dbContext.Disciplinas.Add(new Disciplina(nome));

        DbUpdateException exception = await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync()
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBDisciplina_UserId_Nome");
    }

    [TestMethod]
    public async Task Salvar_MateriasComMesmoNomeParaMesmoUsuario_LancaDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string nome = $"Matéria {Guid.NewGuid():N}";

        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContexto(usuarioId);
        var disciplina = new Disciplina($"Disciplina {Guid.NewGuid():N}");
        dbContext.Materias.Add(new Materia(nome, 7, disciplina));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        Disciplina disciplinaPersistida = await dbContext.Disciplinas.SingleAsync();
        dbContext.Materias.Add(new Materia(nome, 8, disciplinaPersistida));

        DbUpdateException exception = await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync()
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBMateria_UserId_Nome");
    }

    [TestMethod]
    public async Task Salvar_ProvasComMesmoTituloParaMesmoUsuario_LancaDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string titulo = $"Prova {Guid.NewGuid():N}";

        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContexto(usuarioId);
        var disciplina = new Disciplina($"Disciplina {Guid.NewGuid():N}");
        var materia = new Materia($"Matéria {Guid.NewGuid():N}", 7, disciplina);
        dbContext.Provas.Add(new Prova(titulo, disciplina, materia, 7, 1, false));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        Disciplina disciplinaPersistida = await dbContext.Disciplinas.SingleAsync();
        Materia materiaPersistida = await dbContext.Materias.SingleAsync();
        dbContext.Provas.Add(new Prova(titulo, disciplinaPersistida, materiaPersistida, 7, 2, false));

        DbUpdateException exception = await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync()
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBProva_UserId_Titulo");
    }

    [TestMethod]
    public async Task Salvar_MesmosNomesETitulosParaUsuariosDiferentes_PersisteTodos()
    {
        Guid primeiroUsuarioId = Guid.CreateVersion7();
        Guid segundoUsuarioId = Guid.CreateVersion7();
        string nomeDisciplina = $"Disciplina compartilhada {Guid.NewGuid():N}";
        string nomeMateria = $"Matéria compartilhada {Guid.NewGuid():N}";
        string tituloProva = $"Prova compartilhada {Guid.NewGuid():N}";

        await CadastrarGrafoAsync(primeiroUsuarioId, nomeDisciplina, nomeMateria, tituloProva);
        await CadastrarGrafoAsync(segundoUsuarioId, nomeDisciplina, nomeMateria, tituloProva);

        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContextoSemUsuario();

        Assert.AreEqual(2, await dbContext.Disciplinas.IgnoreQueryFilters().CountAsync());
        Assert.AreEqual(2, await dbContext.Materias.IgnoreQueryFilters().CountAsync());
        Assert.AreEqual(2, await dbContext.Provas.IgnoreQueryFilters().CountAsync());
    }

    [TestMethod]
    public async Task Salvar_DisciplinasConcorrentesComMesmoNome_UmaFalhaComDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string nome = $"Disciplina concorrente {Guid.NewGuid():N}";

        await using GeradorDeProvasDbContext primeiroContexto = Fixture.CriarContexto(usuarioId);
        await using GeradorDeProvasDbContext segundoContexto = Fixture.CriarContexto(usuarioId);
        primeiroContexto.Disciplinas.Add(new Disciplina(nome));
        segundoContexto.Disciplinas.Add(new Disciplina(nome));

        DbUpdateException exception = await PersistirConcorrentementeAsync(
            primeiroContexto,
            segundoContexto
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBDisciplina_UserId_Nome");
    }

    [TestMethod]
    public async Task Salvar_MateriasConcorrentesComMesmoNome_UmaFalhaComDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string nome = $"Matéria concorrente {Guid.NewGuid():N}";
        Guid disciplinaId;

        await using (GeradorDeProvasDbContext preparacao = Fixture.CriarContexto(usuarioId))
        {
            var disciplina = new Disciplina($"Disciplina {Guid.NewGuid():N}");
            preparacao.Disciplinas.Add(disciplina);
            await preparacao.SaveChangesAsync();
            disciplinaId = disciplina.Id;
        }

        await using GeradorDeProvasDbContext primeiroContexto = Fixture.CriarContexto(usuarioId);
        await using GeradorDeProvasDbContext segundoContexto = Fixture.CriarContexto(usuarioId);
        Disciplina primeiraDisciplina = await primeiroContexto.Disciplinas.SingleAsync(d => d.Id == disciplinaId);
        Disciplina segundaDisciplina = await segundoContexto.Disciplinas.SingleAsync(d => d.Id == disciplinaId);
        primeiroContexto.Materias.Add(new Materia(nome, 7, primeiraDisciplina));
        segundoContexto.Materias.Add(new Materia(nome, 8, segundaDisciplina));

        DbUpdateException exception = await PersistirConcorrentementeAsync(
            primeiroContexto,
            segundoContexto
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBMateria_UserId_Nome");
    }

    [TestMethod]
    public async Task Salvar_ProvasConcorrentesComMesmoTitulo_UmaFalhaComDbUpdateException()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string titulo = $"Prova concorrente {Guid.NewGuid():N}";
        Guid disciplinaId;
        Guid materiaId;

        await using (GeradorDeProvasDbContext preparacao = Fixture.CriarContexto(usuarioId))
        {
            var disciplina = new Disciplina($"Disciplina {Guid.NewGuid():N}");
            var materia = new Materia($"Matéria {Guid.NewGuid():N}", 7, disciplina);
            preparacao.Materias.Add(materia);
            await preparacao.SaveChangesAsync();
            disciplinaId = disciplina.Id;
            materiaId = materia.Id;
        }

        await using GeradorDeProvasDbContext primeiroContexto = Fixture.CriarContexto(usuarioId);
        await using GeradorDeProvasDbContext segundoContexto = Fixture.CriarContexto(usuarioId);
        Disciplina primeiraDisciplina = await primeiroContexto.Disciplinas.SingleAsync(d => d.Id == disciplinaId);
        Disciplina segundaDisciplina = await segundoContexto.Disciplinas.SingleAsync(d => d.Id == disciplinaId);
        Materia primeiraMateria = await primeiroContexto.Materias.SingleAsync(m => m.Id == materiaId);
        Materia segundaMateria = await segundoContexto.Materias.SingleAsync(m => m.Id == materiaId);
        primeiroContexto.Provas.Add(new Prova(titulo, primeiraDisciplina, primeiraMateria, 7, 1, false));
        segundoContexto.Provas.Add(new Prova(titulo, segundaDisciplina, segundaMateria, 7, 1, false));

        DbUpdateException exception = await PersistirConcorrentementeAsync(
            primeiroContexto,
            segundoContexto
        );

        AssertViolacaoIndiceUnico(exception, "UQ_TBProva_UserId_Titulo");
    }

    private static async Task CadastrarGrafoAsync(
        Guid usuarioId,
        string nomeDisciplina,
        string nomeMateria,
        string tituloProva
    )
    {
        await using GeradorDeProvasDbContext dbContext = Fixture.CriarContexto(usuarioId);
        var disciplina = new Disciplina(nomeDisciplina);
        var materia = new Materia(nomeMateria, 7, disciplina);
        dbContext.Provas.Add(new Prova(tituloProva, disciplina, materia, 7, 1, false));

        await dbContext.SaveChangesAsync();
    }

    private static async Task<DbUpdateException> PersistirConcorrentementeAsync(
        GeradorDeProvasDbContext primeiroContexto,
        GeradorDeProvasDbContext segundoContexto
    )
    {
        Task<int> primeiraPersistencia = primeiroContexto.SaveChangesAsync();
        Task<int> segundaPersistencia = segundoContexto.SaveChangesAsync();
        Task<int>[] persistencias = [primeiraPersistencia, segundaPersistencia];

        try
        {
            await Task.WhenAll(persistencias);
        }
        catch (DbUpdateException)
        {
            // A exceção é validada abaixo junto ao resultado da operação concorrente.
        }

        Assert.AreEqual(1, persistencias.Count(task => task.IsCompletedSuccessfully));
        Assert.AreEqual(1, persistencias.Count(task => task.IsFaulted));

        Exception exception = persistencias
            .Single(task => task.IsFaulted)
            .Exception!
            .InnerExceptions
            .Single();

        return Assert.IsInstanceOfType<DbUpdateException>(exception);
    }

    private static void AssertViolacaoIndiceUnico(
        DbUpdateException exception,
        string nomeIndice
    )
    {
        SqlException sqlException = Assert.IsInstanceOfType<SqlException>(
            exception.GetBaseException()
        );

        Assert.IsTrue(sqlException.Number is 2601 or 2627);
        StringAssert.Contains(sqlException.Message, nomeIndice);
    }
}
