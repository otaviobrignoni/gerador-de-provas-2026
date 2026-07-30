using System.Linq.Expressions;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloQuestao;

public sealed class RepositorioQuestao(
    GeradorDeProvasDbContext dbContext
) : RepositorioBase<Questao>(dbContext), IRepositorioQuestao
{
    public override Questao? SelecionarPorId(Guid idSelecionado)
    {
        return registros
            .Include(q => q.Materia)
            .Include(q => q.Alternativas)
            .SingleOrDefault(q => q.Id == idSelecionado);
    }

    public override List<Questao> SelecionarTodos(Expression<Func<Questao, bool>>? filtro = null)
    {
        return [.. registros
            .Include(q => q.Materia)
            .Include(q => q.Alternativas)
            .Where(filtro ?? (_ => true))
        ];
    }
}
