using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;

namespace GeradorDeProvas.Aplicacao.ModuloMateria;

public static class MateriaMappingExtensions
{
    public static Materia ParaEntidade(this CadastrarMateriaDto dto, Disciplina disciplina)
    {
        return new Materia(dto.Nome, dto.Serie, disciplina);
    }

    public static Materia ParaEntidade(this EditarMateriaDto dto, Disciplina disciplina)
    {
        return new Materia(dto.Nome, dto.Serie, disciplina);
    }

    public static List<ListarMateriaDto> ParaListarDto(this IEnumerable<Materia> materias)
    {
        return [.. materias.Select(m => new ListarMateriaDto(
            m.Id,
            m.Nome,
            m.Serie,
            m.Disciplina.Nome
        ))];
    }

    public static DetalhesMateriaDto ParaDetalhesDto(this Materia materia)
    {
        return new DetalhesMateriaDto(
            materia.Id,
            materia.Nome,
            materia.Serie,
            materia.Disciplina.Id,
            materia.Disciplina.Nome
        );
    }

    public static List<OpcaoDisciplinaMateriaDto> ParaOpcoesDto(this IEnumerable<Disciplina> disciplinas)
    {
        return [.. disciplinas.Select(d => new OpcaoDisciplinaMateriaDto(d.Id, d.Nome))];
    }
}
