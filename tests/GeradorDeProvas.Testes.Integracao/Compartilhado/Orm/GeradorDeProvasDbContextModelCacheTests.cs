using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Testes.Integracao.Compartilhado.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Orm;

[TestClass]
[TestCategory("Security")]
[TestCategory("Infrastructure")]
public sealed class GeradorDeProvasDbContextModelCacheTests
{
    [TestMethod]
    public void QueryFilter_ModeloCriadoSemProvedor_NaoExpoeDadosEntreUsuarios()
    {
        string banco = $"model-cache-{Guid.CreateVersion7():N}";
        using ServiceProvider servicosEf = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();
        var options = new DbContextOptionsBuilder<GeradorDeProvasDbContext>()
            .UseInMemoryDatabase(banco)
            .UseInternalServiceProvider(servicosEf)
            .Options;

        using (var contextoSemProvedor = new GeradorDeProvasDbContext(options))
            _ = contextoSemProvedor.Model;

        Guid usuarioId = Guid.CreateVersion7();
        Guid outroUsuarioId = Guid.CreateVersion7();
        SalvarDisciplina(options, usuarioId, "Disciplina do usuário");
        SalvarDisciplina(options, outroUsuarioId, "Disciplina alheia");

        using var contextoUsuario = new GeradorDeProvasDbContext(
            options,
            new FalsoProvedorDeUsuario(usuarioId)
        );

        Disciplina disciplina = contextoUsuario.Disciplinas.Single();

        Assert.AreEqual("Disciplina do usuário", disciplina.Nome);
        Assert.AreEqual(usuarioId, disciplina.UserId);
    }

    private static void SalvarDisciplina(
        DbContextOptions<GeradorDeProvasDbContext> options,
        Guid usuarioId,
        string nome
    )
    {
        using var contexto = new GeradorDeProvasDbContext(
            options,
            new FalsoProvedorDeUsuario(usuarioId)
        );
        contexto.Add(new Disciplina(nome));
        contexto.SaveChanges();
    }
}
