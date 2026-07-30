using GeradorDeProvas.Dominio.ModuloDisciplina;

namespace GeradorDeProvas.Aplicacao.ModuloDisciplina;

public static class DisciplinaMappingExtensions
{
    public static Disciplina ParaEntidade(this CadastrarDisciplinaDto dto)
    {
        return new Disciplina(dto.Nome);
    }

    public static Disciplina ParaEntidade(this EditarDisciplinaDto dto)
    {
        return new Disciplina(dto.Nome);
    }

    public static List<ListarDisciplinaDto> ParaListarDto(this IEnumerable<Disciplina> disciplinas)
    {
        return [.. disciplinas.Select(d => new ListarDisciplinaDto(d.Id, d.Nome))];
    }

    public static DetalhesDisciplinaDto ParaDetalhesDto(this Disciplina disciplina)
    {
        return new DetalhesDisciplinaDto(disciplina.Id, disciplina.Nome);
    }
}
