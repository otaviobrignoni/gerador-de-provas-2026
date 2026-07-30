using System.Linq.Expressions;

namespace GeradorDeProvas.Dominio.Compartilhado;

public interface IRepositorio<T> where T : EntidadeBase<T>
{
    void Cadastrar(T entidade);
    bool Editar(Guid idSelecionado, T entidadeAtualizada);
    bool Excluir(Guid idSelecionado);
    T? SelecionarPorId(Guid idSelecionado);
    List<T> SelecionarTodos(Expression<Func<T, bool>>? filtro = null);
}
