using System.Linq.Expressions;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloProva;

public sealed class RepositorioProva(GeradorDeProvasDbContext dbContext) : RepositorioBase<Prova>(dbContext), IRepositorioProva
{
    public override Prova? SelecionarPorId(Guid idSelecionado)
    {
        return registros
            .Include(p => p.Disciplina)
            .Include(p => p.Materia)
            .Include(p => p.Questoes)
                .ThenInclude(q => q.Alternativas)
            .SingleOrDefault(p => p.Id == idSelecionado);
    }

    public override List<Prova> SelecionarTodos(Expression<Func<Prova, bool>>? filtro = null)
    {
        return [.. registros
            .Include(p => p.Disciplina)
            .Include(p => p.Materia)
            .Include(p => p.Questoes)
                .ThenInclude(q => q.Alternativas)
            .Where(filtro ?? (_ => true))
        ];
    }
}
