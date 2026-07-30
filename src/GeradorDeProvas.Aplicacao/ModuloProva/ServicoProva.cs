using FluentResults;
using GeradorDeProvas.Aplicacao.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao.ModuloProva;

public sealed class ServicoProva(IRepositorioProva repositorioProva, IRepositorioDisciplina repositorioDisciplina, IRepositorioMateria repositorioMateria, IRepositorioQuestao repositorioQuestao) : ServicoBase<Prova>
{
    public Result Cadastrar(CadastrarProvaDto dto) => Cadastrar(dto, null);

    public Result Cadastrar(CadastrarProvaDto dto, List<Guid>? questaoIds)
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
            return Falha(string.Empty, "Prova não encontrada.");

        Prova provaDuplicada = new(
            dto.Titulo,
            provaOriginal.Disciplina,
            provaOriginal.Materia,
            provaOriginal.Serie,
            provaOriginal.QuantidadeQuestoes,
            provaOriginal.ProvaRecuperacao
        );

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
            return Falha(string.Empty, "Prova não encontrada.");

        return Result.Ok();
    }

    public List<ListarProvaDto> SelecionarTodos()
    {
        return [.. repositorioProva
            .SelecionarTodos()
            .Select(p => new ListarProvaDto(
                p.Id,
                p.Titulo,
                p.Disciplina.Nome,
                p.Materia?.Nome,
                p.QuantidadeQuestoes,
                p.ProvaRecuperacao
            ))];
    }

    public Result<DetalhesProvaDto> SelecionarPorId(Guid id)
    {
        Prova? prova = repositorioProva.SelecionarPorId(id);

        if (prova is null)
            return Result.Fail("Prova não encontrada.");

        return Result.Ok(MapearDetalhes(prova));
    }

    public Result<List<QuestaoProvaDto>> SortearQuestoes(CadastrarProvaDto dto)
    {
        Result<Prova> resultadoProva = PrepararProva(dto, null);

        if (resultadoProva.IsFailed)
            return Result.Fail<List<QuestaoProvaDto>>(resultadoProva.Errors);

        return Result.Ok(resultadoProva.Value.Questoes.Select(MapearQuestao).ToList());
    }

    public Result<List<QuestaoProvaDto>> SelecionarQuestoes(IEnumerable<Guid> ids)
    {
        List<Guid> idsOrdenados = [.. ids];
        List<Questao> questoes = [.. repositorioQuestao
            .SelecionarTodos()
            .Where(q => idsOrdenados.Contains(q.Id))
        ];

        if (questoes.Count != idsOrdenados.Count)
            return Result.Fail<List<QuestaoProvaDto>>("Uma ou mais questões não foram encontradas.");

        Dictionary<Guid, Questao> porId = questoes.ToDictionary(q => q.Id);

        return Result.Ok(idsOrdenados.Select(id => MapearQuestao(porId[id])).ToList());
    }

    public List<OpcaoDisciplinaProvaDto> SelecionarDisciplinas()
    {
        return [.. repositorioDisciplina
            .SelecionarTodos()
            .Select(d => new OpcaoDisciplinaProvaDto(d.Id, d.Nome))
        ];
    }

    public List<OpcaoMateriaProvaDto> SelecionarMaterias(Guid disciplinaId, int serie)
    {
        return [.. repositorioMateria
            .SelecionarTodos()
            .Where(m => m.Disciplina.Id == disciplinaId && m.Serie == serie)
            .Select(m => new OpcaoMateriaProvaDto(m.Id, m.Nome, m.Serie))
        ];
    }

    public List<OpcaoMateriaProvaDto> SelecionarMaterias(Guid disciplinaId)
    {
        return [.. repositorioMateria
            .SelecionarTodos()
            .Where(m => m.Disciplina.Id == disciplinaId)
            .Select(m => new OpcaoMateriaProvaDto(m.Id, m.Nome, m.Serie))
        ];
    }

    private Result<Disciplina> SelecionarDisciplina(Guid id)
    {
        Disciplina? disciplina = repositorioDisciplina.SelecionarPorId(id);
        if (disciplina is null)
            return Result.Fail<Disciplina>(new Error("Selecione uma disciplina válida.").WithMetadata("Campo", nameof(CadastrarProvaDto.DisciplinaId)));

        return Result.Ok(disciplina);
    }

    private Result<Prova> PrepararProva(CadastrarProvaDto dto, IReadOnlyCollection<Guid>? questaoIds)
    {
        if (ExisteProvaComMesmoTitulo(dto.Titulo))
            return Result.Fail<Prova>(new Error("Já existe uma prova com este título.").WithMetadata("Campo", nameof(dto.Titulo)));

        Result<Disciplina> resultadoDisciplina = SelecionarDisciplina(dto.DisciplinaId);

        if (resultadoDisciplina.IsFailed)
            return resultadoDisciplina.ToResult();

        Result<Materia?> resultadoMateria = SelecionarMateria(dto.DisciplinaId, dto.MateriaId, dto.ProvaRecuperacao);

        if (resultadoMateria.IsFailed)
            return resultadoMateria.ToResult();

        List<Materia> materiasElegiveis = [.. repositorioMateria
            .SelecionarTodos()
            .Where(m => m.Disciplina.Id == dto.DisciplinaId && m.Serie == dto.Serie)
            .Where(m => dto.ProvaRecuperacao || m.Id == dto.MateriaId)
        ];

        Prova prova = new(
            dto.Titulo,
            resultadoDisciplina.Value,
            resultadoMateria.Value,
            dto.Serie,
            dto.QuantidadeQuestoes,
            dto.ProvaRecuperacao
        );

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
            Result<List<Questao>> resultadoQuestoes = SelecionarQuestoes(
                materiasElegiveis,
                dto.QuantidadeQuestoes,
                questaoIds
            );

            if (resultadoQuestoes.IsFailed)
                return resultadoQuestoes.ToResult();

            prova.Questoes = resultadoQuestoes.Value;
        }

        return Result.Ok(prova);
    }

    private bool ExisteProvaComMesmoTitulo(string titulo, Guid? idIgnorado = null)
    {
        string tituloNormalizado = NormalizarTitulo(titulo);

        return repositorioProva.SelecionarTodos().Any(p => p.Id != idIgnorado && NormalizarTitulo(p.Titulo) == tituloNormalizado);
    }

    private static string NormalizarTitulo(string titulo) => titulo.Trim().ToLowerInvariant();

    private static DetalhesProvaDto MapearDetalhes(Prova prova)
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
            [.. prova.Questoes.Select(q => new QuestaoProvaDto(
                q.Id,
                q.Enunciado,
                [.. q.Alternativas.Select(a => new AlternativaProvaDto(a.Id, a.Texto, a.Correta))]
            ))]
        );
    }

    private Result<Materia?> SelecionarMateria(Guid disciplinaId, Guid? materiaId, bool recuperacao)
    {
        if (recuperacao)
            return Result.Ok<Materia?>(null);

        if (!materiaId.HasValue)
            return Result.Fail<Materia?>(new Error("Selecione uma matéria válida.").WithMetadata("Campo", nameof(CadastrarProvaDto.MateriaId)));

        Materia? materia = repositorioMateria.SelecionarPorId(materiaId.Value);

        if (materia is null || materia.Disciplina.Id != disciplinaId)
            return Result.Fail<Materia?>(new Error("A matéria selecionada não pertence à disciplina informada.").WithMetadata("Campo", nameof(CadastrarProvaDto.MateriaId)));

        return Result.Ok<Materia?>(materia);
    }

    private List<Questao> SelecionarQuestoesElegiveis(List<Materia> materiasElegiveis)
    {
        List<Guid> idsMaterias = [.. materiasElegiveis.Select(m => m.Id)];

        return [.. repositorioQuestao
            .SelecionarTodos()
            .Where(q => idsMaterias.Contains(q.Materia.Id))
        ];
    }

    private Result<List<Questao>> SelecionarQuestoes(
        List<Materia> materiasElegiveis,
        int quantidade,
        IReadOnlyCollection<Guid> questaoIds
    )
    {
        if (questaoIds.Count != quantidade || questaoIds.Distinct().Count() != questaoIds.Count)
            return Result.Fail<List<Questao>>(new Error("A quantidade de questões confirmada é inválida.").WithMetadata("Campo", nameof(CadastrarProvaDto.QuantidadeQuestoes)));

        HashSet<Guid> idsMaterias = [.. materiasElegiveis.Select(m => m.Id)];
        List<Questao> questoes = [.. repositorioQuestao
            .SelecionarTodos()
            .Where(q => questaoIds.Contains(q.Id) && idsMaterias.Contains(q.Materia.Id))
        ];

        if (questoes.Count != quantidade)
            return Result.Fail<List<Questao>>(new Error("Uma ou mais questões confirmadas não pertencem à configuração da prova.").WithMetadata("Campo", nameof(CadastrarProvaDto.QuantidadeQuestoes)));

        Dictionary<Guid, Questao> porId = questoes.ToDictionary(q => q.Id);
        return Result.Ok(questaoIds.Select(id => porId[id]).ToList());
    }

    private static QuestaoProvaDto MapearQuestao(Questao questao)
    {
        return new QuestaoProvaDto(
            questao.Id,
            questao.Enunciado,
            [.. questao.Alternativas.Select(a => new AlternativaProvaDto(a.Id, a.Texto, a.Correta))]
        );
    }
}
