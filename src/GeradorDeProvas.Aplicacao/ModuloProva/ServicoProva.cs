using FluentResults;
using GeradorDeProvas.Aplicacao.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloProva;

public sealed class ServicoProva(IRepositorioProva repositorioProva, IRepositorioDisciplina repositorioDisciplina, IRepositorioMateria repositorioMateria, IRepositorioQuestao repositorioQuestao) : ServicoBase<Prova>
{
    public Result Cadastrar(CadastrarProvaDto dto, List<Guid>? questaoIds = null)
    {
        Result<Prova> resultadoProva = PrepararProva(dto, questaoIds);

        if (resultadoProva.IsFailed)
            return resultadoProva.ToResult();

        repositorioProva.Cadastrar(resultadoProva.Value);

        return Result.Ok();
    }

    public Result Duplicar(DuplicarProvaDto dto)
    {
        if (ExisteProvaComMesmoTitulo(dto.Titulo))
            return Falha(nameof(dto.Titulo), "Já existe uma prova com este título.");

        Prova? provaOriginal = repositorioProva.SelecionarPorId(dto.Id);

        if (provaOriginal is null)
            return Result.Fail("Prova não encontrada.");

        Prova provaDuplicada = provaOriginal.ParaCopia(dto.Titulo);

        Result resultadoValidacao = ValidarEntidade(provaDuplicada);

        if (resultadoValidacao.IsFailed)
            return resultadoValidacao;

        repositorioProva.Cadastrar(provaDuplicada);

        return Result.Ok();
    }

    public Result Excluir(Guid id)
    {
        bool conseguiuExcluir = repositorioProva.Excluir(id);

        if (!conseguiuExcluir)
            return Result.Fail("Prova não encontrada.");

        return Result.Ok();
    }

    public List<ListarProvaDto> SelecionarTodos()
    {
        return repositorioProva.SelecionarTodos().ParaListarDto();
    }

    public Result<DetalhesProvaDto> SelecionarPorId(Guid id)
    {
        Prova? prova = repositorioProva.SelecionarPorId(id);

        if (prova is null)
            return Result.Fail("Prova não encontrada.");

        return Result.Ok(prova.ParaDetalhesDto());
    }

    public Result<List<QuestaoProvaDto>> SortearQuestoes(CadastrarProvaDto dto)
    {
        Result<Prova> resultadoProva = PrepararProva(dto, null);

        if (resultadoProva.IsFailed)
            return Result.Fail<List<QuestaoProvaDto>>(resultadoProva.Errors);

        return Result.Ok(resultadoProva.Value.Questoes.ParaQuestoesDto());
    }

    public Result<List<QuestaoProvaDto>> SelecionarQuestoes(IEnumerable<Guid> ids)
    {
        List<Guid> idsOrdenados = [.. ids];
        List<Questao> questoes = repositorioQuestao
            .SelecionarTodos(q => idsOrdenados.Contains(q.Id));

        if (questoes.Count != idsOrdenados.Count)
            return Result.Fail<List<QuestaoProvaDto>>("Uma ou mais questões não foram encontradas.");

        Dictionary<Guid, Questao> porId = questoes.ToDictionary(q => q.Id);

        return Result.Ok(idsOrdenados.Select(id => porId[id].ParaQuestaoDto()).ToList());
    }

    public List<OpcaoDisciplinaProvaDto> SelecionarDisciplinas()
    {
        return repositorioDisciplina.SelecionarTodos().ParaOpcoesDto();
    }

    public List<OpcaoMateriaProvaDto> SelecionarMaterias(Guid disciplinaId, int serie)
    {
        return SelecionarMateriasFiltradas(disciplinaId, serie);
    }

    public List<OpcaoMateriaProvaDto> SelecionarMaterias(Guid disciplinaId)
    {
        return SelecionarMateriasFiltradas(disciplinaId, null);
    }

    private List<OpcaoMateriaProvaDto> SelecionarMateriasFiltradas(Guid disciplinaId, int? serie)
    {
        return repositorioMateria
            .SelecionarTodos(m => m.Disciplina.Id == disciplinaId
                && (!serie.HasValue || m.Serie == serie))
            .ParaOpcoesDto();
    }

    private Result<Disciplina> SelecionarDisciplina(Guid id)
    {
        Disciplina? disciplina = repositorioDisciplina.SelecionarPorId(id);
        if (disciplina is null)
            return Falha<Disciplina>(nameof(CadastrarProvaDto.DisciplinaId), "Selecione uma disciplina válida.");

        return Result.Ok(disciplina);
    }

    private Result<Prova> PrepararProva(CadastrarProvaDto dto, IReadOnlyCollection<Guid>? questaoIds)
    {
        if (ExisteProvaComMesmoTitulo(dto.Titulo))
            return Falha<Prova>(nameof(dto.Titulo), "Já existe uma prova com este título.");

        Result<Disciplina> resultadoDisciplina = SelecionarDisciplina(dto.DisciplinaId);

        if (resultadoDisciplina.IsFailed)
            return resultadoDisciplina.ToResult();

        Result<Materia?> resultadoMateria = SelecionarMateria(dto.DisciplinaId, dto.MateriaId, dto.ProvaRecuperacao);

        if (resultadoMateria.IsFailed)
            return resultadoMateria.ToResult();

        List<Materia> materiasElegiveis = repositorioMateria
            .SelecionarTodos(m => m.Disciplina.Id == dto.DisciplinaId
                && m.Serie == dto.Serie
                && (dto.ProvaRecuperacao || m.Id == dto.MateriaId));

        Prova prova = dto.ParaEntidade(resultadoDisciplina.Value, resultadoMateria.Value);

        Result resultadoValidacao = ValidarEntidade(prova);

        if (resultadoValidacao.IsFailed)
            return Result.Fail<Prova>(resultadoValidacao.Errors);

        if (questaoIds is null)
        {
            List<Questao> disponiveis = SelecionarQuestoesElegiveis(materiasElegiveis);
            List<string> errosSorteio = prova.SortearQuestoes(disponiveis);

            if (errosSorteio.Count > 0)
                return Result.Fail<Prova>(errosSorteio);
        }
        else
        {
            Result<List<Questao>> resultadoQuestoes = SelecionarQuestoes(materiasElegiveis, dto.QuantidadeQuestoes, questaoIds);

            if (resultadoQuestoes.IsFailed)
                return resultadoQuestoes.ToResult();

            prova.Questoes = resultadoQuestoes.Value;
        }

        return Result.Ok(prova);
    }

    private bool ExisteProvaComMesmoTitulo(string titulo, Guid? idIgnorado = null)
    {
        string tituloNormalizado = titulo.Normalizar();

        return repositorioProva
            .SelecionarTodos(p => (!idIgnorado.HasValue || p.Id != idIgnorado.Value)
                && p.Titulo.Trim().ToLower() == tituloNormalizado)
            .Any();
    }

    private Result<Materia?> SelecionarMateria(Guid disciplinaId, Guid? materiaId, bool recuperacao)
    {
        if (recuperacao)
            return Result.Ok<Materia?>(null);

        if (!materiaId.HasValue)
            return Falha<Materia?>(nameof(CadastrarProvaDto.MateriaId), "Selecione uma matéria válida.");

        Materia? materia = repositorioMateria.SelecionarPorId(materiaId.Value);

        if (materia is null || materia.Disciplina.Id != disciplinaId)
            return Falha<Materia?>(nameof(CadastrarProvaDto.MateriaId), "A matéria selecionada não pertence à disciplina informada.");

        return Result.Ok<Materia?>(materia);
    }

    private List<Questao> SelecionarQuestoesElegiveis(List<Materia> materiasElegiveis)
    {
        List<Guid> idsMaterias = [.. materiasElegiveis.Select(m => m.Id)];

        return repositorioQuestao
            .SelecionarTodos(q => idsMaterias.Contains(q.Materia.Id));
    }

    private Result<List<Questao>> SelecionarQuestoes(List<Materia> materiasElegiveis, int quantidade, IReadOnlyCollection<Guid> questaoIds)
    {
        if (questaoIds.Count != quantidade || questaoIds.Distinct().Count() != questaoIds.Count)
            return Falha<List<Questao>>(nameof(CadastrarProvaDto.QuantidadeQuestoes), "A quantidade de questões confirmada é inválida.");

        HashSet<Guid> idsMaterias = [.. materiasElegiveis.Select(m => m.Id)];
        List<Questao> questoes = repositorioQuestao
            .SelecionarTodos(q => questaoIds.Contains(q.Id)
                && idsMaterias.Contains(q.Materia.Id));

        if (questoes.Count != quantidade)
            return Falha<List<Questao>>(nameof(CadastrarProvaDto.QuantidadeQuestoes), "Uma ou mais questões confirmadas não pertencem à configuração da prova.");

        Dictionary<Guid, Questao> porId = questoes.ToDictionary(q => q.Id);
        return Result.Ok(questaoIds.Select(id => porId[id]).ToList());
    }
}
