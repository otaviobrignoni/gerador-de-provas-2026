using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Infra.ModuloQuestao;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm.Database;

public sealed partial class SqlServerDatabaseTests
{
    [TestMethod]
    public void ExcluirDisciplina_VinculadaAMateria_EhRestritoPeloSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        var disciplina = new Disciplina(CriarNomeRelacionalUnico("Disciplina materia"));
        var materia = new Materia(CriarNomeRelacionalUnico("Materia vinculada"), 1, disciplina);

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(materia);
            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            Disciplina disciplinaPersistida = contexto.Disciplinas.Single(d => d.Id == disciplina.Id);
            contexto.Remove(disciplinaPersistida);

            Assert.ThrowsExactly<DbUpdateException>(() => contexto.SaveChanges());
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Assert.AreEqual(disciplina.Id, contextoVerificacao.Disciplinas.Single().Id);
        Assert.AreEqual(materia.Id, contextoVerificacao.Materias.Single().Id);
    }

    [TestMethod]
    public void ExcluirDisciplina_VinculadaDiretamenteAProva_EhRestritoPeloSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        var disciplina = new Disciplina(CriarNomeRelacionalUnico("Disciplina prova"));
        var prova = new Prova(
            CriarNomeRelacionalUnico("Prova recuperacao"),
            disciplina,
            null,
            1,
            1,
            true
        );

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(prova);
            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            Disciplina disciplinaPersistida = contexto.Disciplinas.Single(d => d.Id == disciplina.Id);
            contexto.Remove(disciplinaPersistida);

            Assert.ThrowsExactly<DbUpdateException>(() => contexto.SaveChanges());
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Assert.AreEqual(disciplina.Id, contextoVerificacao.Disciplinas.Single().Id);
        Assert.AreEqual(prova.Id, contextoVerificacao.Provas.Single().Id);
    }

    [TestMethod]
    public void ExcluirMateria_VinculadaAQuestao_EhRestritoPeloSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        GrafoQuestaoRelacional grafo = CriarGrafoQuestaoRelacional();

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(grafo.Questao);
            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            Materia materiaPersistida = contexto.Materias.Single(m => m.Id == grafo.Materia.Id);
            contexto.Remove(materiaPersistida);

            Assert.ThrowsExactly<DbUpdateException>(() => contexto.SaveChanges());
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Assert.AreEqual(grafo.Materia.Id, contextoVerificacao.Materias.Single().Id);
        Assert.AreEqual(grafo.Questao.Id, contextoVerificacao.Questoes.Single().Id);
    }

    [TestMethod]
    public void ExcluirMateria_VinculadaDiretamenteAProva_EhRestritoPeloSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        var disciplina = new Disciplina(CriarNomeRelacionalUnico("Disciplina prova materia"));
        var materia = new Materia(CriarNomeRelacionalUnico("Materia prova"), 2, disciplina);
        var prova = new Prova(
            CriarNomeRelacionalUnico("Prova materia"),
            disciplina,
            materia,
            2,
            1,
            false
        );

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(prova);
            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            Materia materiaPersistida = contexto.Materias.Single(m => m.Id == materia.Id);
            contexto.Remove(materiaPersistida);

            Assert.ThrowsExactly<DbUpdateException>(() => contexto.SaveChanges());
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Assert.AreEqual(materia.Id, contextoVerificacao.Materias.Single().Id);
        Assert.AreEqual(prova.Id, contextoVerificacao.Provas.Single().Id);
    }

    [TestMethod]
    public void ExcluirQuestao_RemoveAlternativasEmCascadeNoSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        GrafoQuestaoRelacional grafo = CriarGrafoQuestaoRelacional();
        Guid[] alternativasIds = [.. grafo.Questao.Alternativas.Select(a => a.Id)];

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(grafo.Questao);
            contexto.SaveChanges();
            contexto.ChangeTracker.Clear();

            Questao questaoPersistida = contexto.Questoes.Single(q => q.Id == grafo.Questao.Id);
            contexto.Remove(questaoPersistida);
            contexto.SaveChanges();
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Assert.IsFalse(contextoVerificacao.Questoes.Any(q => q.Id == grafo.Questao.Id));
        Assert.IsFalse(
            contextoVerificacao.Alternativas
                .IgnoreQueryFilters()
                .Any(a => alternativasIds.Contains(a.Id))
        );
        Assert.AreEqual(grafo.Materia.Id, contextoVerificacao.Materias.Single().Id);
    }

    [TestMethod]
    public void AssociacaoProvaQuestao_PersisteERemoveVinculo_SemExcluirEntidades()
    {
        Guid usuarioId = Guid.CreateVersion7();
        GrafoCompletoRelacional grafo = CriarGrafoCompletoRelacional();

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(grafo.Prova);
            contexto.SaveChanges();
        }

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            Prova provaPersistida = contexto.Provas
                .Include(p => p.Questoes)
                .Single(p => p.Id == grafo.Prova.Id);

            Assert.AreEqual(grafo.Questao.Id, provaPersistida.Questoes.Single().Id);

            provaPersistida.Questoes.Clear();
            contexto.SaveChanges();
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Prova provaSemQuestoes = contextoVerificacao.Provas
            .Include(p => p.Questoes)
            .Single(p => p.Id == grafo.Prova.Id);
        Assert.IsEmpty(provaSemQuestoes.Questoes);
        Assert.AreEqual(grafo.Questao.Id, contextoVerificacao.Questoes.Single().Id);
    }

    [TestMethod]
    public void EditarQuestao_RemoveAlternativasAntigasEPersisteNovasComUserIdCorreto()
    {
        Guid usuarioId = Guid.CreateVersion7();
        GrafoQuestaoRelacional grafo = CriarGrafoQuestaoRelacional();
        Guid[] alternativasAntigasIds = [.. grafo.Questao.Alternativas.Select(a => a.Id)];

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            contexto.Add(grafo.Questao);
            contexto.SaveChanges();
        }

        var novasAlternativas = new List<Alternativa>
        {
            new(CriarNomeRelacionalUnico("Nova correta"), true),
            new(CriarNomeRelacionalUnico("Nova incorreta A"), false),
            new(CriarNomeRelacionalUnico("Nova incorreta B"), false)
        };
        Guid[] novasAlternativasIds = [.. novasAlternativas.Select(a => a.Id)];
        string novoEnunciado = CriarNomeRelacionalUnico("Enunciado editado");

        using (var contexto = Fixture.CriarContexto(usuarioId))
        {
            Materia materiaPersistida = contexto.Materias.Single(m => m.Id == grafo.Materia.Id);
            var questaoAtualizada = new Questao(novoEnunciado, materiaPersistida, novasAlternativas);
            var repositorio = new RepositorioQuestao(contexto);

            bool conseguiuEditar = repositorio.Editar(grafo.Questao.Id, questaoAtualizada);

            Assert.IsTrue(conseguiuEditar);
        }

        using var contextoVerificacao = Fixture.CriarContexto(usuarioId);
        Questao questaoPersistida = contextoVerificacao.Questoes
            .Include(q => q.Alternativas)
            .Single(q => q.Id == grafo.Questao.Id);
        Guid[] idsPersistidos = [.. questaoPersistida.Alternativas.Select(a => a.Id)];

        Assert.AreEqual(novoEnunciado, questaoPersistida.Enunciado);
        CollectionAssert.AreEquivalent(novasAlternativasIds, idsPersistidos);
        Assert.AreEqual(3, questaoPersistida.Alternativas.Count);
        Assert.AreEqual(1, questaoPersistida.Alternativas.Count(a => a.Correta));
        Assert.IsTrue(questaoPersistida.Alternativas.All(a => a.UserId == usuarioId));
        Assert.IsFalse(
            contextoVerificacao.Alternativas
                .IgnoreQueryFilters()
                .Any(a => alternativasAntigasIds.Contains(a.Id))
        );
    }

    [TestMethod]
    public void QueryFilters_IsolamTodosOsTiposDeEntidadePorUsuarioNoSqlServer()
    {
        Guid primeiroUsuarioId = Guid.CreateVersion7();
        Guid segundoUsuarioId = Guid.CreateVersion7();
        GrafoCompletoRelacional primeiroGrafo = CriarGrafoCompletoRelacional();
        GrafoCompletoRelacional segundoGrafo = CriarGrafoCompletoRelacional();

        using (var contexto = Fixture.CriarContexto(primeiroUsuarioId))
        {
            contexto.Add(primeiroGrafo.Prova);
            contexto.SaveChanges();
        }

        using (var contexto = Fixture.CriarContexto(segundoUsuarioId))
        {
            contexto.Add(segundoGrafo.Prova);
            contexto.SaveChanges();
        }

        using (var contexto = Fixture.CriarContexto(primeiroUsuarioId))
            AssertGrafoVisivel(contexto, primeiroGrafo, primeiroUsuarioId);

        using (var contexto = Fixture.CriarContexto(segundoUsuarioId))
            AssertGrafoVisivel(contexto, segundoGrafo, segundoUsuarioId);
    }

    [TestMethod]
    public void SalvarMateria_RelacionadaADisciplinaDeOutroUsuario_EhBloqueado()
    {
        Guid proprietarioId = Guid.CreateVersion7();
        Guid outroUsuarioId = Guid.CreateVersion7();
        var disciplina = new Disciplina(CriarNomeRelacionalUnico("Disciplina proprietaria"));

        using (var contexto = Fixture.CriarContexto(proprietarioId))
        {
            contexto.Add(disciplina);
            contexto.SaveChanges();
        }

        Guid materiaId;
        using (var contexto = Fixture.CriarContexto(outroUsuarioId))
        {
            Disciplina disciplinaAlheia = contexto.Disciplinas
                .IgnoreQueryFilters()
                .Single(d => d.Id == disciplina.Id);
            var materia = new Materia(CriarNomeRelacionalUnico("Materia indevida"), 1, disciplinaAlheia);
            materiaId = materia.Id;
            contexto.Add(materia);

            var excecao = Assert.ThrowsExactly<UnauthorizedAccessException>(
                () => contexto.SaveChanges()
            );
            Assert.AreEqual(
                "Não é permitido relacionar entidades pertencentes a usuários diferentes.",
                excecao.Message
            );
        }

        using var contextoVerificacao = Fixture.CriarContextoSemUsuario();
        Assert.IsFalse(
            contextoVerificacao.Materias
                .IgnoreQueryFilters()
                .Any(m => m.Id == materiaId)
        );
    }

    [TestMethod]
    public void AssociarProvaAQuestaoDeOutroUsuario_EhBloqueado()
    {
        Guid proprietarioProvaId = Guid.CreateVersion7();
        Guid proprietarioQuestaoId = Guid.CreateVersion7();
        var disciplinaProva = new Disciplina(CriarNomeRelacionalUnico("Disciplina da prova"));
        var prova = new Prova(
            CriarNomeRelacionalUnico("Prova proprietaria"),
            disciplinaProva,
            null,
            1,
            1,
            true
        );
        GrafoQuestaoRelacional grafoQuestao = CriarGrafoQuestaoRelacional();

        using (var contexto = Fixture.CriarContexto(proprietarioProvaId))
        {
            contexto.Add(prova);
            contexto.SaveChanges();
        }

        using (var contexto = Fixture.CriarContexto(proprietarioQuestaoId))
        {
            contexto.Add(grafoQuestao.Questao);
            contexto.SaveChanges();
        }

        using (var contexto = Fixture.CriarContexto(proprietarioProvaId))
        {
            Prova provaPersistida = contexto.Provas.Single(p => p.Id == prova.Id);
            Questao questaoAlheia = contexto.Questoes
                .IgnoreQueryFilters()
                .Single(q => q.Id == grafoQuestao.Questao.Id);
            provaPersistida.Questoes.Add(questaoAlheia);

            var excecao = Assert.ThrowsExactly<UnauthorizedAccessException>(
                () => contexto.SaveChanges()
            );
            Assert.AreEqual(
                "Não é permitido relacionar entidades pertencentes a usuários diferentes.",
                excecao.Message
            );
        }

        using var contextoVerificacao = Fixture.CriarContexto(proprietarioProvaId);
        Prova provaSemQuestaoAlheia = contextoVerificacao.Provas
            .Include(p => p.Questoes)
            .Single(p => p.Id == prova.Id);
        Assert.IsEmpty(provaSemQuestaoAlheia.Questoes);
    }

    private static void AssertGrafoVisivel(
        GeradorDeProvasDbContext contexto,
        GrafoCompletoRelacional grafoEsperado,
        Guid usuarioEsperadoId
    )
    {
        Disciplina disciplina = contexto.Disciplinas.Single();
        Materia materia = contexto.Materias.Single();
        Questao questao = contexto.Questoes.Single();
        Prova prova = contexto.Provas.Single();
        Alternativa[] alternativas = [.. contexto.Alternativas.OrderBy(a => a.Id)];

        Assert.AreEqual(grafoEsperado.Disciplina.Id, disciplina.Id);
        Assert.AreEqual(grafoEsperado.Materia.Id, materia.Id);
        Assert.AreEqual(grafoEsperado.Questao.Id, questao.Id);
        Assert.AreEqual(grafoEsperado.Prova.Id, prova.Id);
        CollectionAssert.AreEquivalent(
            grafoEsperado.Questao.Alternativas.Select(a => a.Id).ToArray(),
            alternativas.Select(a => a.Id).ToArray()
        );
        IEntidadeDoUsuario[] entidades = [disciplina, materia, questao, prova, .. alternativas];
        Assert.IsTrue(entidades.All(e => e.UserId == usuarioEsperadoId));
    }

    private static GrafoQuestaoRelacional CriarGrafoQuestaoRelacional()
    {
        var disciplina = new Disciplina(CriarNomeRelacionalUnico("Disciplina"));
        var materia = new Materia(CriarNomeRelacionalUnico("Materia"), 1, disciplina);
        var questao = new Questao(
            CriarNomeRelacionalUnico("Enunciado"),
            materia,
            [
                new Alternativa(CriarNomeRelacionalUnico("Alternativa correta"), true),
                new Alternativa(CriarNomeRelacionalUnico("Alternativa incorreta"), false)
            ]
        );

        return new GrafoQuestaoRelacional(disciplina, materia, questao);
    }

    private static GrafoCompletoRelacional CriarGrafoCompletoRelacional()
    {
        GrafoQuestaoRelacional grafoQuestao = CriarGrafoQuestaoRelacional();
        var prova = new Prova(
            CriarNomeRelacionalUnico("Prova"),
            grafoQuestao.Disciplina,
            grafoQuestao.Materia,
            1,
            1,
            false,
            [grafoQuestao.Questao]
        );

        return new GrafoCompletoRelacional(
            grafoQuestao.Disciplina,
            grafoQuestao.Materia,
            grafoQuestao.Questao,
            prova
        );
    }

    private static string CriarNomeRelacionalUnico(string prefixo) =>
        $"{prefixo} {Guid.CreateVersion7():N}";

    private sealed record GrafoQuestaoRelacional(
        Disciplina Disciplina,
        Materia Materia,
        Questao Questao
    );

    private sealed record GrafoCompletoRelacional(
        Disciplina Disciplina,
        Materia Materia,
        Questao Questao,
        Prova Prova
    );
}
