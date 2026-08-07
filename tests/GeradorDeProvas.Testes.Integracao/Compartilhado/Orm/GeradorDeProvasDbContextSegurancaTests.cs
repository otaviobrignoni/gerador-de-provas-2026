using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Identity;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

[TestClass]
public sealed class GeradorDeProvasDbContextSegurancaTests
{
    [TestMethod]
    public void SaveChanges_EntidadeSemUsuario_AtribuiUsuarioAutenticado()
    {
        var usuarioId = Guid.CreateVersion7();
        using var dbContext = CriarContexto(CriarNomeDoBanco(), usuarioId);
        var disciplina = new Disciplina("Matematica");

        dbContext.Disciplinas.Add(disciplina);
        dbContext.SaveChanges();

        Assert.AreEqual(usuarioId, disciplina.UserId);
    }

    [TestMethod]
    public void SaveChanges_CriacaoParaOutroUsuario_LancaExcecao()
    {
        var usuarioId = Guid.CreateVersion7();
        using var dbContext = CriarContexto(CriarNomeDoBanco(), usuarioId);
        var disciplina = new Disciplina("Matematica")
        {
            UserId = Guid.CreateVersion7()
        };
        dbContext.Disciplinas.Add(disciplina);

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => dbContext.SaveChanges());
    }

    [TestMethod]
    public void SaveChanges_SemUsuarioAutenticado_LancaExcecao()
    {
        using var dbContext = CriarContexto(CriarNomeDoBanco());
        dbContext.Disciplinas.Add(new Disciplina("Matematica"));

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => dbContext.SaveChanges());
    }

    [TestMethod]
    public void SaveChanges_AlteracaoDoUsuarioDaEntidade_LancaExcecao()
    {
        var usuarioId = Guid.CreateVersion7();
        using var dbContext = CriarContexto(CriarNomeDoBanco(), usuarioId);
        var disciplina = new Disciplina("Matematica");
        dbContext.Disciplinas.Add(disciplina);
        dbContext.SaveChanges();

        disciplina.UserId = Guid.CreateVersion7();

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => dbContext.SaveChanges());
    }

    [TestMethod]
    public void SaveChanges_EdicaoDeEntidadeAlheia_LancaExcecao()
    {
        var banco = CriarNomeDoBanco();
        var proprietarioId = Guid.CreateVersion7();
        var outroUsuarioId = Guid.CreateVersion7();
        var disciplinaId = SalvarDisciplina(banco, proprietarioId);

        using var dbContext = CriarContexto(banco, outroUsuarioId);
        var disciplina = dbContext.Disciplinas
            .IgnoreQueryFilters()
            .Single(d => d.Id == disciplinaId);
        disciplina.Nome = "Nome alterado";

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => dbContext.SaveChanges());
    }

    [TestMethod]
    public void SaveChanges_ExclusaoDeEntidadeAlheia_LancaExcecao()
    {
        var banco = CriarNomeDoBanco();
        var proprietarioId = Guid.CreateVersion7();
        var outroUsuarioId = Guid.CreateVersion7();
        var disciplinaId = SalvarDisciplina(banco, proprietarioId);

        using var dbContext = CriarContexto(banco, outroUsuarioId);
        var disciplina = dbContext.Disciplinas
            .IgnoreQueryFilters()
            .Single(d => d.Id == disciplinaId);
        dbContext.Disciplinas.Remove(disciplina);

        Assert.ThrowsExactly<UnauthorizedAccessException>(() => dbContext.SaveChanges());
    }

    [TestMethod]
    public void QueryFilters_RetornamApenasEntidadesDoUsuarioAutenticado()
    {
        var banco = CriarNomeDoBanco();
        var usuarioId = Guid.CreateVersion7();
        var outroUsuarioId = Guid.CreateVersion7();
        var entidadesEsperadas = SalvarGrafo(banco, usuarioId, "Usuario");
        SalvarGrafo(banco, outroUsuarioId, "OutroUsuario");

        using var dbContext = CriarContexto(banco, usuarioId);

        Assert.AreEqual(entidadesEsperadas.DisciplinaId, dbContext.Disciplinas.Single().Id);
        Assert.AreEqual(entidadesEsperadas.MateriaId, dbContext.Materias.Single().Id);
        Assert.AreEqual(entidadesEsperadas.QuestaoId, dbContext.Questoes.Single().Id);
        Assert.AreEqual(entidadesEsperadas.AlternativaId, dbContext.Alternativas.Single().Id);
        Assert.AreEqual(entidadesEsperadas.ProvaId, dbContext.Provas.Single().Id);
    }

    [TestMethod]
    public async Task SaveChangesAsync_EntidadeSemUsuario_AtribuiUsuarioAutenticado()
    {
        var usuarioId = Guid.CreateVersion7();
        await using var dbContext = CriarContexto(CriarNomeDoBanco(), usuarioId);
        var disciplina = new Disciplina("Matematica");
        dbContext.Disciplinas.Add(disciplina);

        await dbContext.SaveChangesAsync();

        Assert.AreEqual(usuarioId, disciplina.UserId);
    }

    [TestMethod]
    public async Task SaveChangesAsync_CriacaoParaOutroUsuario_LancaExcecao()
    {
        var usuarioId = Guid.CreateVersion7();
        await using var dbContext = CriarContexto(CriarNomeDoBanco(), usuarioId);
        var disciplina = new Disciplina("Matematica")
        {
            UserId = Guid.CreateVersion7()
        };
        dbContext.Disciplinas.Add(disciplina);

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => dbContext.SaveChangesAsync()
        );
    }

    [TestMethod]
    public async Task SaveChangesAsync_SemUsuarioAutenticado_LancaExcecao()
    {
        await using var dbContext = CriarContexto(CriarNomeDoBanco());
        dbContext.Disciplinas.Add(new Disciplina("Matematica"));

        await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(
            () => dbContext.SaveChangesAsync()
        );
    }

    private static GeradorDeProvasDbContext CriarContexto(string nomeDoBanco, Guid? usuarioId = null)
    {
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase(nomeDoBanco)
            .Options;
        IProvedorDeUsuario? provedor = usuarioId.HasValue
            ? new FalsoProvedorDeUsuario(usuarioId.Value)
            : null;

        return new GeradorDeProvasDbContext(options, provedor);
    }

    private static string CriarNomeDoBanco() => $"seguranca-{Guid.CreateVersion7():N}";

    private static Guid SalvarDisciplina(string banco, Guid usuarioId)
    {
        using var dbContext = CriarContexto(banco, usuarioId);
        var disciplina = new Disciplina("Matematica");
        dbContext.Disciplinas.Add(disciplina);
        dbContext.SaveChanges();

        return disciplina.Id;
    }

    private static EntidadesIds SalvarGrafo(string banco, Guid usuarioId, string sufixo)
    {
        using var dbContext = CriarContexto(banco, usuarioId);
        var disciplina = new Disciplina($"Disciplina {sufixo}");
        var materia = new Materia($"Materia {sufixo}", 1, disciplina);
        var alternativa = new Alternativa($"Alternativa {sufixo}", true);
        var questao = new Questao($"Questao {sufixo}", materia, [alternativa]);
        var prova = new Prova($"Prova {sufixo}", disciplina, materia, 1, 1, false, [questao]);

        dbContext.AddRange(disciplina, materia, questao, alternativa, prova);
        dbContext.SaveChanges();

        return new EntidadesIds(
            disciplina.Id,
            materia.Id,
            questao.Id,
            alternativa.Id,
            prova.Id
        );
    }

    private sealed record EntidadesIds(
        Guid DisciplinaId,
        Guid MateriaId,
        Guid QuestaoId,
        Guid AlternativaId,
        Guid ProvaId
    );
}
