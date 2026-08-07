using System.Linq.Expressions;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloProva;

public sealed class RepositorioProva(GeradorDeProvasDbContext dbContext) : RepositorioBase<Prova>(dbContext), IRepositorioProva
{
    private const string NomeAssociacaoProvaQuestao = "TBProvaQuestao";
    private readonly GeradorDeProvasDbContext dbContext = dbContext;

    public override Prova? SelecionarPorId(Guid idSelecionado)
    {
        Prova? prova = registros
            .Include(p => p.Disciplina)
            .Include(p => p.Materia)
            .Include(p => p.Questoes)
                .ThenInclude(q => q.Alternativas)
            .SingleOrDefault(p => p.Id == idSelecionado);

        OrdenarQuestoes(prova is null ? [] : [prova]);

        return prova;
    }

    public override List<Prova> SelecionarTodos(Expression<Func<Prova, bool>>? filtro = null)
    {
        List<Prova> provas = [.. registros
            .Include(p => p.Disciplina)
            .Include(p => p.Materia)
            .Include(p => p.Questoes)
                .ThenInclude(q => q.Alternativas)
            .Where(filtro ?? (_ => true))
        ];

        OrdenarQuestoes(provas);

        return provas;
    }

    private void OrdenarQuestoes(List<Prova> provas)
    {
        if (provas.Count == 0)
            return;

        Guid[] provaIds = [.. provas.Select(p => p.Id)];
        var ordens = dbContext.Set<Dictionary<string, object>>(NomeAssociacaoProvaQuestao)
            .Where(associacao => provaIds.Contains(EF.Property<Guid>(associacao, "ProvasId")))
            .Select(associacao => new
            {
                ProvaId = EF.Property<Guid>(associacao, "ProvasId"),
                QuestaoId = EF.Property<Guid>(associacao, "QuestoesId"),
                Ordem = EF.Property<int>(associacao, "Ordem")
            })
            .ToDictionary(
                associacao => (associacao.ProvaId, associacao.QuestaoId),
                associacao => associacao.Ordem
            );

        foreach (Prova prova in provas)
        {
            prova.Questoes = [.. prova.Questoes
                .OrderBy(questao => ordens[(prova.Id, questao.Id)])
            ];
        }
    }
}
