using FluentResults;
using GeradorDeProvas.Dominio.Compartilhado;

namespace GeradorDeProvas.Aplicacao.Compartilhado;

public abstract class ServicoBase<T> where T : EntidadeBase<T>
{
    protected static Result ValidarEntidade(T entidade)
    {
        List<string> erros = entidade.Validar();

        if (erros.Count == 0)
            return Result.Ok();

        return Result.Fail(erros.First());
    }

    protected static Result Falha(string campo, string mensagem) =>
         Result.Fail(new Error(mensagem).WithMetadata("Campo", campo));

    protected static Result<TResultado> Falha<TResultado>(string campo, string mensagem) =>
         Result.Fail<TResultado>(new Error(mensagem).WithMetadata("Campo", campo));
}
