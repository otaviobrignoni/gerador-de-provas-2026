using GeradorDeProvas.Dominio.ModuloQuestao;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GeradorDeProvas.Infra.ModuloQuestao;

public sealed class AlternativaConfiguration : IEntityTypeConfiguration<Alternativa>
{
    public void Configure(EntityTypeBuilder<Alternativa> builder)
    {
        builder.ToTable("TBAlternativa");

        builder.HasKey(a => a.Id)
            .HasName("PK_TBAlternativa");

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.Texto)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.Correta)
            .IsRequired();
    }
}
