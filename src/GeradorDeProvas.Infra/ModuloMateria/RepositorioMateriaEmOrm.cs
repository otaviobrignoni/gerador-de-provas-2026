using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloMateria;

public sealed class RepositorioMateriaEmOrm(
    GeradorDeProvasDbContext dbContext
) : RepositorioBaseEmOrm<Materia>(dbContext), IRepositorioMateria
{
    public override Materia? SelecionarPorId(Guid idSelecionado)
    {
        return registros
            .Include(m => m.Disciplina)
            .SingleOrDefault(m => m.Id == idSelecionado);
    }

    public override List<Materia> SelecionarTodos()
    {
        return registros
            .Include(m => m.Disciplina)
            .ToList();
    }
}
