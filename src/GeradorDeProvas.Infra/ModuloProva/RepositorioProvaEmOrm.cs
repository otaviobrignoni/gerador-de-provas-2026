using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloProva;

public sealed class RepositorioProvaEmOrm(GeradorDeProvasDbContext dbContext) : RepositorioBaseEmOrm<Prova>(dbContext), IRepositorioProva
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

    public override List<Prova> SelecionarTodos()
    {
        return [.. registros
            .Include(p => p.Disciplina)
            .Include(p => p.Materia)
            .Include(p => p.Questoes)
                .ThenInclude(q => q.Alternativas)
        ];
    }
}
