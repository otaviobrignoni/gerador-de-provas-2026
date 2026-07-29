using System.Reflection;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
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
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<Questao> Questoes => Set<Questao>();
    public DbSet<Alternativa> Alternativas => Set<Alternativa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        Assembly assembly = typeof(GeradorDeProvasDbContext).Assembly;

        modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        // Query Filters devem utilizar a dependência do UserProvider diretamente
        // O EF faz cachê do OnModelCreating e variáveis locais não são atualizadas
        if (userProvider is not null)
        {
            modelBuilder.Entity<Disciplina>()
                .HasQueryFilter(d => d.UserId == userProvider.Id);

            modelBuilder.Entity<Materia>()
                .HasQueryFilter(m => m.UserId == userProvider.Id);

            modelBuilder.Entity<Questao>()
                .HasQueryFilter(q => q.UserId == userProvider.Id);

            modelBuilder.Entity<Alternativa>()
                .HasQueryFilter(a => a.UserId == userProvider.Id);
        }
    }

    public override int SaveChanges()
    {
        Guid? userId = userProvider?.Id;

        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException(
                "Não é possível salvar entidades do usuário sem estar autenticado."
            );
        }

        foreach (var entry in ChangeTracker.Entries<IEntidadeDoUsuario>())
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
                        .OriginalValue is Guid idAtual
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

        return base.SaveChanges();
    }
}
