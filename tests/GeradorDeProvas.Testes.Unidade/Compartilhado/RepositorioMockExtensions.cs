using System.Linq.Expressions;
using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.Compartilhado;

public static class RepositorioMockExtensions
{
    public static void ConfigurarSelecao(this Mock<IRepositorioDisciplina> repositorio, params Disciplina[] registros)
    {
        ConfigurarSelecao<IRepositorioDisciplina, Disciplina>(repositorio, registros);
    }

    public static void ConfigurarSelecao(this Mock<IRepositorioMateria> repositorio, params Materia[] registros)
    {
        ConfigurarSelecao<IRepositorioMateria, Materia>(repositorio, registros);
    }

    public static void ConfigurarSelecao(this Mock<IRepositorioQuestao> repositorio, params Questao[] registros)
    {
        ConfigurarSelecao<IRepositorioQuestao, Questao>(repositorio, registros);
    }

    public static void ConfigurarSelecao(this Mock<IRepositorioProva> repositorio, params Prova[] registros)
    {
        ConfigurarSelecao<IRepositorioProva, Prova>(repositorio, registros);
    }

    private static void ConfigurarSelecao<TRepositorio, TEntidade>(Mock<TRepositorio> repositorio, IReadOnlyCollection<TEntidade> registros) where TRepositorio : class, IRepositorio<TEntidade> where TEntidade : EntidadeBase<TEntidade>
    {
        repositorio.Setup(r => r.SelecionarTodos(It.IsAny<Expression<Func<TEntidade, bool>>?>())).Returns((Expression<Func<TEntidade, bool>>? filtro) => [.. registros.Where(filtro?.Compile() ?? (_ => true))]);
    }
}
