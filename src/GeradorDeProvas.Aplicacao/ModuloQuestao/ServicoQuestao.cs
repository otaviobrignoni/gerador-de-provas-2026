using FluentResults;
using GeradorDeProvas.Aplicacao.Compartilhado;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloQuestao;

public class ServicoQuestao(IRepositorioQuestao repositorioQuestao, IRepositorioMateria repositorioMateria, IRepositorioProva? repositorioProva = null) : ServicoBase<Questao>
{
    public Result Cadastrar(CadastrarQuestaoDto dto)
    {
        Result<Materia> resultadoMateria = SelecionarMateria(dto.MateriaId);

        if (resultadoMateria.IsFailed)
            return resultadoMateria.ToResult();

        Questao novaQuestao = new(
            dto.Enunciado,
            resultadoMateria.Value,
            [.. dto.Alternativas.Select(a => new Alternativa(a.Texto, a.Correta))]
        );

        Result resultadoValidacao = ValidarEntidade(novaQuestao);

        if (resultadoValidacao.IsFailed)
            return resultadoValidacao;

        repositorioQuestao.Cadastrar(novaQuestao);

        return Result.Ok();
    }

    public Result Editar(EditarQuestaoDto dto)
    {
        Result<Materia> resultadoMateria = SelecionarMateria(dto.MateriaId);

        if (resultadoMateria.IsFailed)
            return resultadoMateria.ToResult();

        Questao questaoAtualizada = new(
            dto.Enunciado,
            resultadoMateria.Value,
            [.. dto.Alternativas.Select(a => new Alternativa(a.Texto, a.Correta))]
        );

        Result resultadoValidacao = ValidarEntidade(questaoAtualizada);

        if (resultadoValidacao.IsFailed)
            return resultadoValidacao;

        bool conseguiuEditar = repositorioQuestao.Editar(dto.Id, questaoAtualizada);

        if (!conseguiuEditar)
            return Falha(string.Empty, "Questão não encontrada.");

        return Result.Ok();
    }

    public Result Excluir(Guid id)
    {
        Questao? questao = repositorioQuestao.SelecionarPorId(id);

        if (questao == null)
            return Falha(string.Empty, "Questão não encontrada.");

        if (PossuiProvasVinculadas(questao.Id))
            return Falha(string.Empty, "Não é possível excluir esta questão, pois ela está vinculada a uma prova.");

        repositorioQuestao.Excluir(id);

        return Result.Ok();
    }

    public List<ListarQuestaoDto> SelecionarTodos()
    {
        return [.. repositorioQuestao
            .SelecionarTodos()
            .Select(q => new ListarQuestaoDto(
                q.Id,
                q.Enunciado,
                q.Materia.Nome,
                q.Alternativas.FirstOrDefault(a => a.Correta)?.Texto ?? string.Empty
            ))
        ];
    }

    public Result<DetalhesQuestaoDto> SelecionarPorId(Guid id)
    {
        Questao? questao = repositorioQuestao.SelecionarPorId(id);

        if (questao == null)
            return Result.Fail("Questão não encontrada.");

        return Result.Ok(new DetalhesQuestaoDto(
            questao.Id,
            questao.Enunciado,
            questao.Materia.Id,
            questao.Materia.Nome,
            [.. questao.Alternativas.Select(a => new AlternativaDto(a.Id, a.Texto, a.Correta))]
        ));
    }

    public List<OpcaoMateriaQuestaoDto> SelecionarMaterias()
    {
        return [.. repositorioMateria
            .SelecionarTodos()
            .Select(m => new OpcaoMateriaQuestaoDto(m.Id, m.Nome))
        ];
    }

    private Result<Materia> SelecionarMateria(Guid materiaId)
    {
        Materia? materia = repositorioMateria.SelecionarPorId(materiaId);

        if (materia == null)
            return Result.Fail<Materia>(new Error("Selecione uma matéria válida.").WithMetadata("Campo", nameof(CadastrarQuestaoDto.MateriaId)));

        return Result.Ok(materia);
    }

    private bool PossuiProvasVinculadas(Guid questaoId)
    {
        if (repositorioProva is null)
            return false;
        return repositorioProva.SelecionarTodos().Any(p => p.Questoes.Any(q => q.Id == questaoId));
    }
}
