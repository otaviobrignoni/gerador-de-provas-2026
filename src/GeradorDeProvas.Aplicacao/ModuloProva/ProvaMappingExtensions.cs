using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloProva;

public static class ProvaMappingExtensions
{
    public static Prova ParaEntidade(this CadastrarProvaDto dto, Disciplina disciplina, Materia? materia)
    {
        return new Prova(
            dto.Titulo,
            disciplina,
            materia,
            dto.Serie,
            dto.QuantidadeQuestoes,
            dto.ProvaRecuperacao
        );
    }

    public static Prova ParaCopia(this Prova prova, string titulo)
    {
        return new Prova(
            titulo,
            prova.Disciplina,
            prova.Materia,
            prova.Serie,
            prova.QuantidadeQuestoes,
            prova.ProvaRecuperacao
        );
    }

    public static List<ListarProvaDto> ParaListarDto(this IEnumerable<Prova> provas)
    {
        return [.. provas.Select(p => new ListarProvaDto(
            p.Id,
            p.Titulo,
            p.Disciplina.Nome,
            p.Materia?.Nome,
            p.QuantidadeQuestoes,
            p.ProvaRecuperacao
        ))];
    }

    public static DetalhesProvaDto ParaDetalhesDto(this Prova prova)
    {
        return new DetalhesProvaDto(
            prova.Id,
            prova.Titulo,
            prova.Disciplina.Id,
            prova.Disciplina.Nome,
            prova.Materia?.Id,
            prova.Materia?.Nome,
            prova.Serie,
            prova.QuantidadeQuestoes,
            prova.ProvaRecuperacao,
            prova.Questoes.ParaQuestoesDto()
        );
    }

    public static List<QuestaoProvaDto> ParaQuestoesDto(this IEnumerable<Questao> questoes)
    {
        return [.. questoes.Select(q => q.ParaQuestaoDto())];
    }

    public static QuestaoProvaDto ParaQuestaoDto(this Questao questao)
    {
        return new QuestaoProvaDto(
            questao.Id,
            questao.Enunciado,
            [.. questao.Alternativas.Select(a => new AlternativaProvaDto(a.Id, a.Texto, a.Correta))]
        );
    }

    public static List<OpcaoDisciplinaProvaDto> ParaOpcoesDto(this IEnumerable<Disciplina> disciplinas)
    {
        return [.. disciplinas.Select(d => new OpcaoDisciplinaProvaDto(d.Id, d.Nome))];
    }

    public static List<OpcaoMateriaProvaDto> ParaOpcoesDto(this IEnumerable<Materia> materias)
    {
        return [.. materias.Select(m => new OpcaoMateriaProvaDto(m.Id, m.Nome, m.Serie))];
    }
}
