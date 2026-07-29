using GeradorDeProvas.Dominio.ModuloMateria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeradorDeProvas.Infra.ModuloMateria;

public sealed class MateriaConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("TBMateria");

        builder.HasKey(m => m.Id)
            .HasName("PK_TBMateria");

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.UserId)
            .IsRequired();

        builder.Property(m => m.Nome)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Serie)
            .IsRequired();

        builder.HasIndex(m => new { m.UserId, m.Nome })
            .IsUnique()
            .HasDatabaseName("UQ_TBMateria_UserId_Nome");

        builder.HasMany(m => m.Questoes)
            .WithOne(q => q.Materia)
            .HasForeignKey("MateriaId")
            .HasConstraintName("FK_TBQuestao_TBMateria")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
