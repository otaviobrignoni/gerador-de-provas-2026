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
[TestCategory("Security")]
[TestCategory("Infrastructure")]
public sealed class GeradorDeProvasDbContextRelacionamentosTests
{
    [TestMethod]
    public void SaveChanges_RelacionamentoComEntidadeDeOutroUsuario_LancaExcecao()
    {
        string banco = $"relacionamentos-{Guid.CreateVersion7():N}";
        Guid proprietarioId = Guid.CreateVersion7();
        Guid outroUsuarioId = Guid.CreateVersion7();

        Guid disciplinaId;
        using (var contextoProprietario = CriarContexto(banco, proprietarioId))
        {
            var disciplina = new Disciplina("Disciplina proprietaria");
            contextoProprietario.Add(disciplina);
            contextoProprietario.SaveChanges();
            disciplinaId = disciplina.Id;
        }

        using var contextoOutroUsuario = CriarContexto(banco, outroUsuarioId);
        Disciplina disciplinaAlheia = contextoOutroUsuario.Disciplinas
            .IgnoreQueryFilters()
            .Single(d => d.Id == disciplinaId);
        var materia = new Materia("Materia indevida", 1, disciplinaAlheia);
        contextoOutroUsuario.Add(materia);

        var excecao = Assert.ThrowsExactly<UnauthorizedAccessException>(
            () => contextoOutroUsuario.SaveChanges()
        );

        Assert.AreEqual(
            "Não é permitido relacionar entidades pertencentes a usuários diferentes.",
            excecao.Message
        );
    }

    [TestMethod]
    public void SaveChanges_AssociacaoProvaQuestaoDeUsuariosDiferentes_LancaExcecao()
    {
        string banco = $"associacao-prova-questao-{Guid.CreateVersion7():N}";
        Guid usuarioProvaId = Guid.CreateVersion7();
        Guid usuarioQuestaoId = Guid.CreateVersion7();

        Guid provaId;
        using (var contextoProva = CriarContexto(banco, usuarioProvaId))
        {
            var disciplina = new Disciplina("Disciplina da prova");
            var materia = new Materia("Materia da prova", 1, disciplina);
            var prova = new Prova("Prova proprietaria", disciplina, materia, 1, 1, false);
            contextoProva.Add(prova);
            contextoProva.SaveChanges();
            provaId = prova.Id;
        }

        Guid questaoId;
        using (var contextoQuestao = CriarContexto(banco, usuarioQuestaoId))
        {
            var disciplina = new Disciplina("Disciplina da questao");
            var materia = new Materia("Materia da questao", 1, disciplina);
            var questao = new Questao("Questao alheia", materia,
            [
                new Alternativa("Correta", true),
                new Alternativa("Incorreta", false)
            ]);
            contextoQuestao.Add(questao);
            contextoQuestao.SaveChanges();
            questaoId = questao.Id;
        }

        using var contexto = CriarContexto(banco, usuarioProvaId);
        Prova provaAtual = contexto.Provas.Single(p => p.Id == provaId);
        Questao questaoAlheia = contexto.Questoes
            .IgnoreQueryFilters()
            .Single(q => q.Id == questaoId);
        provaAtual.Questoes.Add(questaoAlheia);

        var excecao = Assert.ThrowsExactly<UnauthorizedAccessException>(
            () => contexto.SaveChanges()
        );

        Assert.AreEqual(
            "Não é permitido relacionar entidades pertencentes a usuários diferentes.",
            excecao.Message
        );
    }

    private static GeradorDeProvasDbContext CriarContexto(string banco, Guid usuarioId)
    {
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase(banco)
            .Options;
        IProvedorDeUsuario provedor = new FalsoProvedorDeUsuario(usuarioId);

        return new GeradorDeProvasDbContext(options, provedor);
    }
}
