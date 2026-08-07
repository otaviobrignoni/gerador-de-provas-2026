
using GeradorDeProvas.Dominio.ModuloProva;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeradorDeProvas.Infra.ModuloProva;

public sealed class ProvaConfiguration : IEntityTypeConfiguration<Prova>
{
    public void Configure(EntityTypeBuilder<Prova> builder)
    {
        builder.ToTable("TBProva");

        builder.HasKey(p => p.Id)
            .HasName("PK_TBProva");

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.Titulo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Serie)
            .IsRequired();

        builder.Property(p => p.QuantidadeQuestoes)
            .IsRequired();

        builder.Property(p => p.ProvaRecuperacao)
            .IsRequired();

        builder.HasIndex(p => new { p.UserId, p.Titulo })
            .IsUnique()
            .HasDatabaseName("UQ_TBProva_UserId_Titulo");

        builder.HasOne(p => p.Disciplina)
            .WithMany(d => d.Provas)
            .HasForeignKey("DisciplinaId")
            .HasConstraintName("FK_TBProva_TBDisciplina")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Materia)
            .WithMany(m => m.Provas)
            .HasForeignKey("MateriaId")
            .HasConstraintName("FK_TBProva_TBMateria")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Questoes)
            .WithMany(q => q.Provas)
            .UsingEntity<Dictionary<string, object>>(
                "TBProvaQuestao",
                associacao =>
                {
                    associacao.Property<int>("Ordem")
                        .IsRequired();

                    associacao.HasIndex("ProvasId", "Ordem")
                        .HasDatabaseName("IX_TBProvaQuestao_ProvasId_Ordem");
                }
            );
    }
}
