using System.Linq.Expressions;
using GeradorDeProvas.Dominio.Compartilhado;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.Compartilhado.Orm;

public abstract class RepositorioBase<T>(GeradorDeProvasDbContext dbContext) where T : EntidadeBase<T>
{
    protected readonly DbSet<T> registros = dbContext.Set<T>();

    public void Cadastrar(T entidade)
    {
        registros.Add(entidade);
        dbContext.SaveChanges();
    }

    public bool Editar(Guid idSelecionado, T entidadeAtualizada)
    {
        T? entidade = SelecionarPorId(idSelecionado);

        if (entidade == null)
            return false;

        entidade.Atualizar(entidadeAtualizada);
        dbContext.SaveChanges();

        return true;
    }

    public bool Excluir(Guid idSelecionado)
    {
        T? entidade = SelecionarPorId(idSelecionado);

        if (entidade == null)
            return false;

        registros.Remove(entidade);
        dbContext.SaveChanges();

        return true;
    }

    public virtual T? SelecionarPorId(Guid idSelecionado)
    {
        return registros.SingleOrDefault(c => c.Id == idSelecionado);
    }

    public virtual List<T> SelecionarTodos(Expression<Func<T, bool>>? filtro = null)
    {
        return [.. registros.Where(filtro ?? (_ => true))];
    }
}
