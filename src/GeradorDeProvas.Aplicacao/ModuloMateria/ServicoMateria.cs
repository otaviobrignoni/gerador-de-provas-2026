using FluentResults;
using GeradorDeProvas.Aplicacao.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloMateria;

public class ServicoMateria(IRepositorioMateria repositorioMateria, IRepositorioDisciplina repositorioDisciplina, IRepositorioQuestao repositorioQuestao) : ServicoBase<Materia>
{
    public Result Cadastrar(CadastrarMateriaDto dto)
    {
        if (ExisteMateriaComMesmoNome(dto.Nome))
            return Falha(nameof(dto.Nome), "Já existe uma matéria com este nome.");

        Result<Disciplina> resultadoDisciplina = SelecionarDisciplina(dto.DisciplinaId);

        if (resultadoDisciplina.IsFailed)
            return resultadoDisciplina.ToResult();

        Materia novaMateria = dto.ParaEntidade(resultadoDisciplina.Value);

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

        Materia materiaAtualizada = dto.ParaEntidade(resultadoDisciplina.Value);

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
        return repositorioMateria.SelecionarTodos().ParaListarDto();
    }

    public Result<DetalhesMateriaDto> SelecionarPorId(Guid id)
    {
        Materia? materia = repositorioMateria.SelecionarPorId(id);

        if (materia == null)
            return Result.Fail("Matéria não encontrada.");

        return Result.Ok(materia.ParaDetalhesDto());
    }

    public List<OpcaoDisciplinaMateriaDto> SelecionarDisciplinas()
    {
        return repositorioDisciplina.SelecionarTodos().ParaOpcoesDto();
    }

    private Result<Disciplina> SelecionarDisciplina(Guid disciplinaId)
    {
        Disciplina? disciplina = repositorioDisciplina.SelecionarPorId(disciplinaId);

        if (disciplina == null)
            return Falha<Disciplina>(nameof(CadastrarMateriaDto.DisciplinaId), "Selecione uma disciplina válida.");

        return Result.Ok(disciplina);
    }

    private bool ExisteMateriaComMesmoNome(string nome, Guid? idIgnorado = null)
    {
        string nomeNormalizado = nome.Normalizar();

        return repositorioMateria
            .SelecionarTodos(m => (!idIgnorado.HasValue || m.Id != idIgnorado.Value)
                && m.Nome.Trim().ToLower() == nomeNormalizado)
            .Any();
    }

    private bool PossuiQuestoesVinculadas(Guid materiaId)
    {
        return repositorioQuestao
            .SelecionarTodos(q => q.Materia.Id == materiaId)
            .Any();
    }
}
