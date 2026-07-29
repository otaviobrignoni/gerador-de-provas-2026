using GeradorDeProvas.Dominio.ModuloQuestao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeradorDeProvas.Infra.ModuloQuestao;

public sealed class QuestaoConfiguration : IEntityTypeConfiguration<Questao>
{
    public void Configure(EntityTypeBuilder<Questao> builder)
    {
        builder.ToTable("TBQuestao");

        builder.HasKey(q => q.Id)
            .HasName("PK_TBQuestao");

        builder.Property(q => q.Id)
            .ValueGeneratedNever();

        builder.Property(q => q.UserId)
            .IsRequired();

        builder.Property(q => q.Enunciado)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasMany(q => q.Alternativas)
            .WithOne(a => a.Questao)
            .HasForeignKey("QuestaoId")
            .HasConstraintName("FK_TBAlternativa_TBQuestao")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
