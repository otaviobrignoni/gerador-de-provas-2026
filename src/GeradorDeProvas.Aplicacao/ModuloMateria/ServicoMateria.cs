using FluentResults;
using GeradorDeProvas.Aplicacao.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloMateria;

public class ServicoMateria(
    IRepositorioMateria repositorioMateria,
    IRepositorioDisciplina repositorioDisciplina,
    IRepositorioQuestao repositorioQuestao
) : ServicoBase<Materia>
{
    public Result Cadastrar(CadastrarMateriaDto dto)
    {
        if (ExisteMateriaComMesmoNome(dto.Nome))
            return Falha(nameof(dto.Nome), "Já existe uma matéria com este nome.");

        Result<Disciplina> resultadoDisciplina = SelecionarDisciplina(dto.DisciplinaId);

        if (resultadoDisciplina.IsFailed)
            return resultadoDisciplina.ToResult();

        Materia novaMateria = new(dto.Nome, dto.Serie, resultadoDisciplina.Value);

        Result resultadoValidacao = ValidarEntidade(novaMateria);

        if (resultadoValidacao.IsFailed)
            return resultadoValidacao;

        repositorioMateria.Cadastrar(novaMateria);

        return Result.Ok();
    }

    public Result Editar(EditarMateriaDto dto)
    {
        if (ExisteMateriaComMesmoNome(dto.Nome, dto.Id))
            return Falha(nameof(dto.Nome), "Já existe uma matéria com este nome.");

        Result<Disciplina> resultadoDisciplina = SelecionarDisciplina(dto.DisciplinaId);

        if (resultadoDisciplina.IsFailed)
            return resultadoDisciplina.ToResult();

        Materia materiaAtualizada = new(dto.Nome, dto.Serie, resultadoDisciplina.Value);

        Result resultadoValidacao = ValidarEntidade(materiaAtualizada);

        if (resultadoValidacao.IsFailed)
            return resultadoValidacao;

        bool conseguiuEditar = repositorioMateria.Editar(dto.Id, materiaAtualizada);

        if (!conseguiuEditar)
            return Falha(string.Empty, "Matéria não encontrada.");

        return Result.Ok();
    }

    public Result Excluir(Guid id)
    {
        Materia? materia = repositorioMateria.SelecionarPorId(id);

        if (materia == null)
            return Falha(string.Empty, "Matéria não encontrada.");

        if (PossuiQuestoesVinculadas(id))
            return Falha(string.Empty, "Não é possível excluir esta matéria, pois ela possui questões vinculadas.");

        repositorioMateria.Excluir(id);

        return Result.Ok();
    }

    public List<ListarMateriaDto> SelecionarTodos()
    {
        return repositorioMateria
            .SelecionarTodos()
            .Select(m => new ListarMateriaDto(m.Id, m.Nome, m.Serie, m.Disciplina.Nome))
            .ToList();
    }

    public Result<DetalhesMateriaDto> SelecionarPorId(Guid id)
    {
        Materia? materia = repositorioMateria.SelecionarPorId(id);

        if (materia == null)
            return Result.Fail("Matéria não encontrada.");

        return Result.Ok(new DetalhesMateriaDto(
            materia.Id,
            materia.Nome,
            materia.Serie,
            materia.Disciplina.Id,
            materia.Disciplina.Nome
        ));
    }

    public List<OpcaoDisciplinaMateriaDto> SelecionarDisciplinas()
    {
        return repositorioDisciplina
            .SelecionarTodos()
            .Select(d => new OpcaoDisciplinaMateriaDto(d.Id, d.Nome))
            .ToList();
    }

    private Result<Disciplina> SelecionarDisciplina(Guid disciplinaId)
    {
        Disciplina? disciplina = repositorioDisciplina.SelecionarPorId(disciplinaId);

        if (disciplina == null)
        {
            return Result.Fail<Disciplina>(
                new Error("Selecione uma disciplina válida.")
                    .WithMetadata("Campo", nameof(CadastrarMateriaDto.DisciplinaId))
            );
        }

        return Result.Ok(disciplina);
    }

    private bool ExisteMateriaComMesmoNome(string nome, Guid? idIgnorado = null)
    {
        string nomeNormalizado = NormalizarNome(nome);

        return repositorioMateria
            .SelecionarTodos()
            .Any(m => m.Id != idIgnorado && NormalizarNome(m.Nome) == nomeNormalizado);
    }

    private static string NormalizarNome(string nome)
    {
        return nome.Trim().ToLowerInvariant();
    }

    private bool PossuiQuestoesVinculadas(Guid materiaId)
    {
        return repositorioQuestao
            .SelecionarTodos()
            .Any(q => q.Materia.Id == materiaId);
    }
}
