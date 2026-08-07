using System.Collections;
using System.Reflection;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.Infra.Compartilhado.Orm;

public sealed class GeradorDeProvasDbContext(
    DbContextOptions<GeradorDeProvasDbContext> options,
    IProvedorDeUsuario? userProvider = null
) : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    private const string NomeAssociacaoProvaQuestao = "TBProvaQuestao";
    private const string ProvaIdAssociacao = "ProvasId";
    private const string QuestaoIdAssociacao = "QuestoesId";
    private const string OrdemAssociacao = "Ordem";

    private Guid? UsuarioAtualId => userProvider?.Id;

    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Questao> Questoes => Set<Questao>();
    public DbSet<Alternativa> Alternativas => Set<Alternativa>();
    public DbSet<Prova> Provas => Set<Prova>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        Assembly assembly = typeof(GeradorDeProvasDbContext).Assembly;

        modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        // Os filtros fazem parte de todo modelo cacheado e resolvem o usuário por instância.
        // Sem usuário, nenhuma entidade multi-tenant fica visível.
        modelBuilder.Entity<Disciplina>()
            .HasQueryFilter(d => d.UserId == UsuarioAtualId);

        modelBuilder.Entity<Materia>()
            .HasQueryFilter(m => m.UserId == UsuarioAtualId);

        modelBuilder.Entity<Questao>()
            .HasQueryFilter(q => q.UserId == UsuarioAtualId);

        modelBuilder.Entity<Alternativa>()
            .HasQueryFilter(a => a.UserId == UsuarioAtualId);

        modelBuilder.Entity<Prova>()
            .HasQueryFilter(p => p.UserId == UsuarioAtualId);
    }

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepararOrdemDasQuestoes();
        PrepararEntidadesDoUsuario();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        PrepararOrdemDasQuestoes();
        PrepararEntidadesDoUsuario();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepararOrdemDasQuestoes()
    {
        ChangeTracker.DetectChanges();

        var associacoes = ChangeTracker.Entries()
            .Where(entry =>
                entry.Metadata.Name == NomeAssociacaoProvaQuestao &&
                entry.State != EntityState.Deleted
            )
            .ToList();

        if (associacoes.Count == 0)
            return;

        foreach (var provaEntry in ChangeTracker.Entries<Prova>())
        {
            if (provaEntry.State is EntityState.Detached or EntityState.Deleted)
                continue;

            bool provaNova = provaEntry.State == EntityState.Added;
            bool questoesCarregadas = provaEntry.Collection(p => p.Questoes).IsLoaded;
            bool possuiNovaAssociacao = associacoes.Any(entry =>
                entry.State == EntityState.Added &&
                entry.Property(ProvaIdAssociacao).CurrentValue is Guid provaId &&
                provaId == provaEntry.Entity.Id
            );

            if (!provaNova && !questoesCarregadas && !possuiNovaAssociacao)
                continue;

            for (int ordem = 0; ordem < provaEntry.Entity.Questoes.Count; ordem++)
            {
                Guid questaoId = provaEntry.Entity.Questoes[ordem].Id;
                var associacao = associacoes.SingleOrDefault(entry =>
                    entry.Property(ProvaIdAssociacao).CurrentValue is Guid provaId &&
                    provaId == provaEntry.Entity.Id &&
                    entry.Property(QuestaoIdAssociacao).CurrentValue is Guid id &&
                    id == questaoId
                );

                if (associacao is null)
                    continue;

                var propriedadeOrdem = associacao.Property(OrdemAssociacao);

                if (propriedadeOrdem.CurrentValue is not int ordemAtual || ordemAtual != ordem)
                    propriedadeOrdem.CurrentValue = ordem;
            }
        }
    }

    private void PrepararEntidadesDoUsuario()
    {
        var entidadesDoUsuario = ChangeTracker
            .Entries<IEntidadeDoUsuario>()
            .ToList();
        var entradas = entidadesDoUsuario
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        bool possuiAlteracaoDeAssociacao = ChangeTracker
            .Entries()
            .Any(entry => entry.Metadata.Name == NomeAssociacaoProvaQuestao
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        // Identity usa o mesmo contexto antes de existir um usuário autenticado.
        if (entradas.Count == 0 && !possuiAlteracaoDeAssociacao)
            return;

        Guid? userId = userProvider?.Id;

        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Não é possível salvar entidades do usuário sem estar autenticado."
            );
        }

        foreach (var entry in entradas)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.UserId == Guid.Empty)
                    {
                        entry.Property(nameof(IEntidadeDoUsuario.UserId)).CurrentValue = userId.Value;
                    }
                    else if (entry.Entity.UserId != userId.Value)
                    {
                        throw new UnauthorizedAccessException(
                            "Tentativa de criar entidade para outro usuário."
                        );
                    }

                    break;

                case EntityState.Modified:
                    Guid idOriginalInstituicao = entry
                        .Property(nameof(IEntidadeDoUsuario.UserId))
                        .OriginalValue is Guid idOriginal
                        ? idOriginal
                        : Guid.Empty;

                    Guid idAtualInstituicao = entry
                        .Property(nameof(IEntidadeDoUsuario.UserId))
                        .CurrentValue is Guid idAtual
                        ? idAtual
                        : Guid.Empty;

                    if (idOriginalInstituicao != idAtualInstituicao)
                    {
                        throw new UnauthorizedAccessException(
                              "Não é permitido alterar o usuário de uma entidade."
                          );
                    }

                    if (idAtualInstituicao != userId.Value)
                    {
                        throw new UnauthorizedAccessException(
                            "Tentativa de modificar entidade de outro usuário."
                        );
                    }

                    break;

                case EntityState.Deleted:
                    Guid instituicaoOriginal = entry
                        .Property(nameof(IEntidadeDoUsuario.UserId))
                        .OriginalValue is Guid original
                        ? original
                        : Guid.Empty;

                    if (instituicaoOriginal != userId.Value)
                    {
                        throw new UnauthorizedAccessException(
                            "Tentativa de excluir entidade de outro usuário."
                        );
                    }

                    break;

            }
        }

        ValidarRelacionamentosDoUsuario(entidadesDoUsuario);
    }

    private static void ValidarRelacionamentosDoUsuario(
        IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<IEntidadeDoUsuario>> entradas
    )
    {
        foreach (var entrada in entradas)
        {
            foreach (var navegacao in entrada.Navigations)
            {
                IEnumerable<IEntidadeDoUsuario> relacionadas = navegacao.CurrentValue switch
                {
                    IEntidadeDoUsuario entidade => [entidade],
                    IEnumerable colecao => colecao.Cast<object>().OfType<IEntidadeDoUsuario>(),
                    _ => []
                };

                if (relacionadas.Any(entidade => entidade.UserId != entrada.Entity.UserId))
                {
                    throw new UnauthorizedAccessException(
                        "Não é permitido relacionar entidades pertencentes a usuários diferentes."
                    );
                }
            }
        }
    }
}
