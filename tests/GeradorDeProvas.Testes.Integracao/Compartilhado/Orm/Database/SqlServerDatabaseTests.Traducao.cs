using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm.Database;

public sealed partial class SqlServerDatabaseTests
{
    [TestMethod]
    public void ExpressoesDeNormalizacaoESelecao_SaoTraduzidasEExecutadasNoSqlServer()
    {
        Guid usuarioId = Guid.CreateVersion7();
        string sufixo = Guid.CreateVersion7().ToString("N");
        string nomePersistido = $"  MATEMATICA {sufixo}  ";
        string nomeNormalizado = nomePersistido.Trim().ToLowerInvariant();

        using var contexto = Fixture.CriarContexto(usuarioId);
        var disciplina = new Disciplina(nomePersistido);
        var materiaEsperada = new Materia($"Materia esperada {sufixo}", 7, disciplina);
        var materiaOutraSerie = new Materia($"Materia outra serie {sufixo}", 8, disciplina);
        var questaoPrimeira = new Questao(
            $"Primeira questao {sufixo}",
            materiaEsperada,
            [
                new Alternativa($"Correta primeira {sufixo}", true),
                new Alternativa($"Incorreta primeira {sufixo}", false)
            ]
        );
        var questaoSegunda = new Questao(
            $"Segunda questao {sufixo}",
            materiaEsperada,
            [
                new Alternativa($"Correta segunda {sufixo}", true),
                new Alternativa($"Incorreta segunda {sufixo}", false)
            ]
        );

        contexto.AddRange(questaoPrimeira, questaoSegunda, materiaOutraSerie);
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();

        string sqlNormalizacao = contexto.Disciplinas
            .Where(d => d.Nome.Trim().ToLower() == nomeNormalizado)
            .ToQueryString();
        Guid[] idsQuestoes = [questaoSegunda.Id, questaoPrimeira.Id];
        string sqlSelecao = contexto.Questoes
            .Where(q => idsQuestoes.Contains(q.Id) && q.Materia.Id == materiaEsperada.Id)
            .ToQueryString();

        Disciplina disciplinaSelecionada = contexto.Disciplinas.Single(
            d => d.Nome.Trim().ToLower() == nomeNormalizado
        );
        Guid[] materiasSelecionadas = [.. contexto.Materias
            .Where(m => m.Disciplina.Id == disciplina.Id && m.Serie == materiaEsperada.Serie)
            .Select(m => m.Id)
        ];
        Guid[] questoesSelecionadas = [.. contexto.Questoes
            .Where(q => idsQuestoes.Contains(q.Id) && q.Materia.Id == materiaEsperada.Id)
            .Select(q => q.Id)
        ];

        StringAssert.Contains(sqlNormalizacao, "LOWER(");
        StringAssert.Contains(sqlNormalizacao, "TBDisciplina");
        Assert.IsTrue(
            sqlNormalizacao.Contains("TRIM(", StringComparison.OrdinalIgnoreCase)
            || (sqlNormalizacao.Contains("LTRIM(", StringComparison.OrdinalIgnoreCase)
                && sqlNormalizacao.Contains("RTRIM(", StringComparison.OrdinalIgnoreCase))
        );
        StringAssert.Contains(sqlSelecao, "TBQuestao");
        StringAssert.Contains(sqlSelecao, "TBMateria");
        StringAssert.Contains(sqlSelecao, " IN ");

        Assert.AreEqual(disciplina.Id, disciplinaSelecionada.Id);
        CollectionAssert.AreEqual(
            new[] { materiaEsperada.Id },
            materiasSelecionadas
        );
        CollectionAssert.AreEquivalent(
            idsQuestoes,
            questoesSelecionadas
        );
    }
}
