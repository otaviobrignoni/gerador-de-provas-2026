using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Infra.Compartilhado.Orm;

namespace GeradorDeProvas.Infra.ModuloDisciplina;

public sealed class RepositorioDisciplinaEmOrm(
    GeradorDeProvasDbContext dbContext
) : RepositorioBaseEmOrm<Disciplina>(dbContext), IRepositorioDisciplina;
