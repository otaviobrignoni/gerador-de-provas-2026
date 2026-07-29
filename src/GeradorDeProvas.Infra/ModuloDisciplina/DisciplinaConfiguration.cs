using GeradorDeProvas.Dominio.ModuloDisciplina;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeradorDeProvas.Infra.ModuloDisciplina;

public sealed class DisciplinaConfiguration : IEntityTypeConfiguration<Disciplina>
{
    public void Configure(EntityTypeBuilder<Disciplina> builder)
    {
        builder.ToTable("TBDisciplina");

        builder.HasKey(d => d.Id)
            .HasName("PK_TBDisciplina");

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.UserId)
            .IsRequired();

        builder.Property(d => d.Nome)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(d => new { d.UserId, d.Nome })
            .IsUnique()
            .HasDatabaseName("UQ_TBDisciplina_UserId_Nome");

        builder.HasMany(d => d.Materias)
            .WithOne(m => m.Disciplina)
            .HasForeignKey("DisciplinaId")
            .HasConstraintName("FK_TBMateria_TBDisciplina")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
