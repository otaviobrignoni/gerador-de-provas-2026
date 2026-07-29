using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.ModuloQuestao;

public sealed class RepositorioQuestaoEmOrm(
    GeradorDeProvasDbContext dbContext
) : RepositorioBaseEmOrm<Questao>(dbContext), IRepositorioQuestao
{
    public override Questao? SelecionarPorId(Guid idSelecionado)
    {
        return registros
            .Include(q => q.Materia)
            .Include(q => q.Alternativas)
            .SingleOrDefault(q => q.Id == idSelecionado);
    }

    public override List<Questao> SelecionarTodos()
    {
        return registros
            .Include(q => q.Materia)
            .Include(q => q.Alternativas)
            .ToList();
    }
}
