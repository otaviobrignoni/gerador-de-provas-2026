using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Infra.Compartilhado.Orm;

namespace GeradorDeProvas.Infra.ModuloDisciplina;

public sealed class RepositorioDisciplina(GeradorDeProvasDbContext dbContext) : RepositorioBase<Disciplina>(dbContext), IRepositorioDisciplina;
