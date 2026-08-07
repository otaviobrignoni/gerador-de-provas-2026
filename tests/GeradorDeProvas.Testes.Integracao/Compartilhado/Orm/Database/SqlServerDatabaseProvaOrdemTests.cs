using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.ModuloProva;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm.Database;

public sealed partial class SqlServerDatabaseTests
{
    [TestMethod]
    public void Prova_PersisteERecarregaQuestoesNaOrdemDefinida()
    {
        // Arrange
        Guid usuarioId = Guid.CreateVersion7();
        using var contexto = Fixture.CriarContexto(usuarioId);

        var disciplina = new Disciplina($"Disciplina ordem {Guid.CreateVersion7():N}")
        {
            UserId = usuarioId
        };
        var materia = new Materia($"Matéria ordem {Guid.CreateVersion7():N}", 8, disciplina)
        {
            UserId = usuarioId
        };

        Questao primeira = CriarQuestao(
            Guid.Parse("00000000-0000-0000-0000-000000000003"),
            "Terceiro identificador, primeira posição",
            materia,
            usuarioId
        );
        Questao segunda = CriarQuestao(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "Primeiro identificador, segunda posição",
            materia,
            usuarioId
        );
        Questao terceira = CriarQuestao(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "Segundo identificador, terceira posição",
            materia,
            usuarioId
        );
        var ordemEsperada = new[] { primeira.Id, segunda.Id, terceira.Id };
        var prova = new Prova(
            $"Prova ordem {Guid.CreateVersion7():N}",
            disciplina,
            materia,
            materia.Serie,
            ordemEsperada.Length,
            false,
            [primeira, segunda, terceira]
        )
        {
            UserId = usuarioId
        };

        contexto.Provas.Add(prova);
        contexto.SaveChanges();
        contexto.ChangeTracker.Clear();

        // Act
        var associacoesPersistidas = contexto
            .Set<Dictionary<string, object>>("TBProvaQuestao")
            .Where(associacao =>
                EF.Property<Guid>(associacao, "ProvasId") == prova.Id
            )
            .OrderBy(associacao => EF.Property<int>(associacao, "Ordem"))
            .Select(associacao => new
            {
                QuestaoId = EF.Property<Guid>(associacao, "QuestoesId"),
                Ordem = EF.Property<int>(associacao, "Ordem")
            })
            .ToArray();
        Prova? provaRecarregada = new RepositorioProva(contexto).SelecionarPorId(prova.Id);

        // Assert
        CollectionAssert.AreEqual(
            new[] { 0, 1, 2 },
            associacoesPersistidas.Select(associacao => associacao.Ordem).ToArray()
        );
        CollectionAssert.AreEqual(
            ordemEsperada,
            associacoesPersistidas.Select(associacao => associacao.QuestaoId).ToArray()
        );
        Assert.IsNotNull(provaRecarregada);
        CollectionAssert.AreEqual(
            ordemEsperada,
            provaRecarregada.Questoes.Select(q => q.Id).ToArray()
        );
    }

    private static Questao CriarQuestao(
        Guid id,
        string enunciado,
        Materia materia,
        Guid usuarioId
    )
    {
        return new Questao(
            enunciado,
            materia,
            [
                new Alternativa($"Alternativa incorreta {id:N}", false) { UserId = usuarioId },
                new Alternativa($"Alternativa correta {id:N}", true) { UserId = usuarioId }
            ]
        )
        {
            Id = id,
            UserId = usuarioId
        };
    }
}
