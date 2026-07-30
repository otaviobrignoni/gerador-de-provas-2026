using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloQuestao;

public static class QuestaoMappingExtensions
{
    public static Questao ParaEntidade(this CadastrarQuestaoDto dto, Materia materia)
    {
        return new Questao(
            dto.Enunciado,
            materia,
            [.. dto.Alternativas.Select(a => a.ParaEntidade())]
        );
    }

    public static Questao ParaEntidade(this EditarQuestaoDto dto, Materia materia)
    {
        return new Questao(
            dto.Enunciado,
            materia,
            [.. dto.Alternativas.Select(a => a.ParaEntidade())]
        );
    }

    public static Alternativa ParaEntidade(this CadastrarAlternativaDto dto)
    {
        return new Alternativa(dto.Texto, dto.Correta);
    }

    public static List<ListarQuestaoDto> ParaListarDto(this IEnumerable<Questao> questoes)
    {
        return [.. questoes.Select(q => new ListarQuestaoDto(
            q.Id,
            q.Enunciado,
            q.Materia.Nome,
            q.Alternativas.FirstOrDefault(a => a.Correta)?.Texto ?? string.Empty
        ))];
    }

    public static DetalhesQuestaoDto ParaDetalhesDto(this Questao questao)
    {
        return new DetalhesQuestaoDto(
            questao.Id,
            questao.Enunciado,
            questao.Materia.Id,
            questao.Materia.Nome,
            [.. questao.Alternativas.Select(a => new AlternativaDto(a.Id, a.Texto, a.Correta))]
        );
    }

    public static List<OpcaoMateriaQuestaoDto> ParaOpcoesDto(this IEnumerable<Materia> materias)
    {
        return [.. materias.Select(m => new OpcaoMateriaQuestaoDto(m.Id, m.Nome))];
    }
}
