using System.Linq.Expressions;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloMateria;

public sealed class RepositorioMateria(
    GeradorDeProvasDbContext dbContext
) : RepositorioBase<Materia>(dbContext), IRepositorioMateria
{
    public override Materia? SelecionarPorId(Guid idSelecionado)
    {
        return registros
            .Include(m => m.Disciplina)
            .SingleOrDefault(m => m.Id == idSelecionado);
    }

    public override List<Materia> SelecionarTodos(Expression<Func<Materia, bool>>? filtro = null)
    {
        return [.. registros
            .Include(m => m.Disciplina)
            .Where(filtro ?? (_ => true))
        ];
    }
}
