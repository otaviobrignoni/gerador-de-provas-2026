using FizzWare.NBuilder;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Infra.ModuloDisciplina;
using GeradorDeProvas.Infra.ModuloMateria;
using GeradorDeProvas.Infra.ModuloProva;
using GeradorDeProvas.Infra.ModuloQuestao;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Identity;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

public abstract class RepositorioBaseTests
{
    protected GeradorDeProvasDbContext dbContext = null!;

    protected RepositorioDisciplina repositorioDisciplina = null!;
    protected RepositorioMateria repositorioMateria = null!;
    protected RepositorioQuestao repositorioQuestao = null!;
    protected RepositorioProva repositorioProva = null!;

    [TestInitialize]
    public void InicializarContexto()
    {
        var opt = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase("GeradorDeProvasTestDB_Memory")
            .Options;
        dbContext = new GeradorDeProvasDbContext(opt, new FalsoProvedorDeUsuario(Guid.NewGuid()));

        repositorioDisciplina = new(dbContext);
        BuilderSetup.SetCreatePersistenceMethod<Disciplina>(repositorioDisciplina.Cadastrar);
        BuilderSetup.SetCreatePersistenceMethod<IList<Disciplina>>(ds =>
        {
            foreach (var d in ds)
                repositorioDisciplina.Cadastrar(d);
            dbContext.ChangeTracker.Clear();
        });

        repositorioMateria = new(dbContext);
        BuilderSetup.SetCreatePersistenceMethod<Materia>(repositorioMateria.Cadastrar);
        BuilderSetup.SetCreatePersistenceMethod<IList<Materia>>(ms =>
        {
            foreach (var m in ms)
                repositorioMateria.Cadastrar(m);
        });

        repositorioQuestao = new(dbContext);
        BuilderSetup.SetCreatePersistenceMethod<Questao>(repositorioQuestao.Cadastrar);
        BuilderSetup.SetCreatePersistenceMethod<IList<Questao>>(qs =>
        {
            foreach (var q in qs)
                repositorioQuestao.Cadastrar(q);
        });

        repositorioProva = new(dbContext);
        BuilderSetup.SetCreatePersistenceMethod<Prova>(repositorioProva.Cadastrar);
        BuilderSetup.SetCreatePersistenceMethod<IList<Prova>>(ps =>
        {
            foreach (var p in ps)
                repositorioProva.Cadastrar(p);
        });
    }

    [TestCleanup]
    public void DescartarContexto()
    {
        dbContext.Dispose();
    }
}
