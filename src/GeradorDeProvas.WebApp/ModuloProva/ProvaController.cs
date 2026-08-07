using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloProva;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeProvas.WebApp.ModuloProva;


public class ProvaController(
    ServicoProva servicoProva,
    IMapper mapeador,
    ILogger<ProvaController>? logger = null
) : Controller
{
    private const string ConfiguracaoKeyPrefix = "Prova.Configuracao.";
    private const string EstadoGeracaoKeyPrefix = "Prova.EstadoGeracao.";
    private const string MensagemFluxoAusente = "O fluxo de geração da prova não foi encontrado ou expirou.";
    private const string MensagemEstadoInvalido = "O estado da geração da prova é inválido. Revise as opções e sorteie novamente.";

    private sealed record ConfiguracaoProva(
        string Titulo,
        Guid DisciplinaId,
        int Serie,
        bool ProvaRecuperacao
    );

    private sealed record EstadoGeracao(
        string Titulo,
        Guid DisciplinaId,
        Guid? MateriaId,
        int Serie,
        int QuantidadeQuestoes,
        bool ProvaRecuperacao,
        List<Guid> QuestaoIds
    );

    [HttpGet]
    public ActionResult Listar()
    {
        List<ListarProvaDto> dtos = servicoProva.SelecionarTodos();
        return View(mapeador.Map<List<ListarProvaViewModel>>(dtos));
    }

    [HttpGet]
    public ActionResult Cadastrar()
    {
        PreservarFluxosAtivos();
        CadastrarProvaEtapa1ViewModel viewModel = new(string.Empty, null, null, false);
        CarregarDisciplinas();
        return View(viewModel);
    }

    [HttpPost]
    public ActionResult Cadastrar(CadastrarProvaEtapa1ViewModel viewModel)
    {
        PreservarFluxosAtivos();

        if (!ModelState.IsValid)
        {
            CarregarDisciplinas();
            return View(viewModel);
        }

        Guid fluxoId = Guid.CreateVersion7();

        TempData[ObterConfiguracaoKey(fluxoId)] = JsonSerializer.Serialize(new ConfiguracaoProva(
            viewModel.Titulo,
            viewModel.DisciplinaId!.Value,
            viewModel.Serie!.Value,
            viewModel.ProvaRecuperacao
        ));

        return RedirectToAction(nameof(SelecionarQuestoes), new { fluxoId });
    }

    [HttpGet]
    public ActionResult SelecionarQuestoes(Guid fluxoId)
    {
        PreservarFluxosAtivos();

        ConfiguracaoProva? configuracao = LerConfiguracao(fluxoId, out bool configuracaoInvalida);
        bool estadoInvalido = false;
        EstadoGeracao? estadoAnterior = configuracao is null
            ? LerEstadoGeracao(fluxoId, out estadoInvalido)
            : null;

        if (configuracao is null && estadoAnterior is not null)
        {
            configuracao = new ConfiguracaoProva(
                estadoAnterior.Titulo,
                estadoAnterior.DisciplinaId,
                estadoAnterior.Serie,
                estadoAnterior.ProvaRecuperacao
            );

            if (!ConfiguracaoEhValida(configuracao))
                return RedirecionarParaCadastro(MensagemEstadoInvalido, fluxoId);
        }

        if (configuracao is null)
            return RedirecionarParaCadastro(
                configuracaoInvalida || estadoInvalido ? MensagemEstadoInvalido : MensagemFluxoAusente,
                fluxoId
            );

        string? nomeDisciplina = ObterNomeDisciplina(configuracao.DisciplinaId);

        if (nomeDisciplina is null)
            return RedirecionarParaCadastro("A disciplina selecionada não foi encontrada.", fluxoId);

        CarregarMaterias(configuracao.DisciplinaId, configuracao.Serie);
        ViewBag.FluxoId = fluxoId;

        return View(new CadastrarProvaEtapa2ViewModel(
            configuracao.Titulo,
            nomeDisciplina,
            configuracao.Serie,
            configuracao.ProvaRecuperacao,
            estadoAnterior?.MateriaId,
            estadoAnterior?.QuantidadeQuestoes
        ));
    }

    [HttpPost]
    public ActionResult SelecionarQuestoes(CadastrarProvaEtapa2ViewModel viewModel, Guid fluxoId)
    {
        PreservarFluxosAtivos();

        ConfiguracaoProva? configuracao = LerConfiguracao(fluxoId, out bool configuracaoInvalida);
        bool estadoInvalido = false;
        EstadoGeracao? estadoAnterior = configuracao is null
            ? LerEstadoGeracao(fluxoId, out estadoInvalido)
            : null;

        if (configuracao is null && estadoAnterior is not null)
        {
            configuracao = new ConfiguracaoProva(
                estadoAnterior.Titulo,
                estadoAnterior.DisciplinaId,
                estadoAnterior.Serie,
                estadoAnterior.ProvaRecuperacao
            );

            if (!ConfiguracaoEhValida(configuracao))
                return RedirecionarParaCadastro(MensagemEstadoInvalido, fluxoId);
        }

        if (configuracao is null)
            return RedirecionarParaCadastro(
                configuracaoInvalida || estadoInvalido ? MensagemEstadoInvalido : MensagemFluxoAusente,
                fluxoId
            );

        string? nomeDisciplina = ObterNomeDisciplina(configuracao.DisciplinaId);

        if (nomeDisciplina is null)
            return RedirecionarParaCadastro("A disciplina selecionada não foi encontrada.", fluxoId);

        CadastrarProvaEtapa2ViewModel modeloAtual = viewModel with
        {
            Titulo = configuracao.Titulo,
            NomeDisciplina = nomeDisciplina,
            Serie = configuracao.Serie,
            ProvaRecuperacao = configuracao.ProvaRecuperacao
        };

        if (!ModelState.IsValid)
        {
            CarregarMaterias(configuracao.DisciplinaId, configuracao.Serie);
            ViewBag.FluxoId = fluxoId;
            return View(modeloAtual);
        }

        CadastrarProvaDto dto = new(
            configuracao.Titulo,
            configuracao.DisciplinaId,
            viewModel.MateriaId,
            configuracao.Serie,
            viewModel.QuantidadeQuestoes!.Value,
            configuracao.ProvaRecuperacao
        );

        Result<List<QuestaoProvaDto>> resultado = servicoProva.SortearQuestoes(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado.ToResult());
            CarregarMaterias(configuracao.DisciplinaId, configuracao.Serie);
            ViewBag.FluxoId = fluxoId;
            return View(modeloAtual);
        }

        TempData.Remove(ObterConfiguracaoKey(fluxoId));

        TempData[ObterEstadoGeracaoKey(fluxoId)] = JsonSerializer.Serialize(new EstadoGeracao(
            configuracao.Titulo,
            configuracao.DisciplinaId,
            viewModel.MateriaId,
            configuracao.Serie,
            viewModel.QuantidadeQuestoes.Value,
            configuracao.ProvaRecuperacao,
            resultado.Value.Select(q => q.Id).ToList()
        ));

        return RedirectToAction(nameof(Confirmar), new { fluxoId });
    }

    [HttpGet]
    public ActionResult Confirmar(Guid fluxoId)
    {
        PreservarFluxosAtivos();
        EstadoGeracao? estado = LerEstadoGeracao(fluxoId, out bool estadoInvalido);

        if (estado is null)
            return RedirecionarParaCadastro(
                estadoInvalido ? MensagemEstadoInvalido : MensagemFluxoAusente,
                fluxoId
            );

        Result<ConfirmarProvaViewModel> resultado = MontarPrevia(estado);

        if (resultado.IsFailed)
            return RedirecionarAposFalhaNaPrevia(resultado, fluxoId);

        ViewBag.FluxoId = fluxoId;
        return View(resultado.Value);
    }

    [HttpPost]
    public ActionResult Confirmar(IFormCollection _, Guid fluxoId)
    {
        PreservarFluxosAtivos();
        EstadoGeracao? estado = LerEstadoGeracao(fluxoId, out bool estadoInvalido);

        if (estado is null)
            return RedirecionarParaCadastro(
                estadoInvalido ? MensagemEstadoInvalido : MensagemFluxoAusente,
                fluxoId
            );

        Result<ConfirmarProvaViewModel> previa = MontarPrevia(estado);

        if (previa.IsFailed)
            return RedirecionarAposFalhaNaPrevia(previa, fluxoId);

        CadastrarProvaDto dto = new(
            estado.Titulo,
            estado.DisciplinaId,
            estado.MateriaId,
            estado.Serie,
            estado.QuantidadeQuestoes,
            estado.ProvaRecuperacao
        );

        Result resultado;

        try
        {
            resultado = servicoProva.Cadastrar(dto, estado.QuestaoIds);
        }
        catch (DbUpdateException exception)
        {
            logger?.LogError(exception, "Falha ao persistir a prova do fluxo {FluxoId}.", fluxoId);
            resultado = Result.Fail("Não foi possível salvar a prova. Tente confirmar novamente.");
        }

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            ViewBag.FluxoId = fluxoId;
            return View(previa.Value);
        }

        RemoverFluxo(fluxoId);

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Detalhes(Guid id)
    {
        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(id);
        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        return View(mapeador.Map<DetalhesProvaViewModel>(resultado.Value));
    }

    [HttpGet]
    public ActionResult Pdf(Guid id)
    {
        return GerarPdf(id, incluirGabarito: false, prefixoArquivo: "prova");
    }

    [HttpGet]
    public ActionResult Gabarito(Guid id)
    {
        return GerarPdf(id, incluirGabarito: true, prefixoArquivo: "gabarito");
    }

    [HttpGet]
    public ActionResult Duplicar(Guid id)
    {
        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        DuplicarProvaViewModel viewModel = mapeador.Map<DuplicarProvaViewModel>(resultado.Value);

        return View(viewModel with { Titulo = $"{viewModel.Titulo} - Cópia" });
    }

    [HttpPost]
    public ActionResult Duplicar(DuplicarProvaViewModel viewModel)
    {
        if (!ModelState.IsValid)
            return View(viewModel);

        Result resultado = servicoProva.Duplicar(new DuplicarProvaDto(viewModel.Id, viewModel.Titulo));

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Excluir(Guid id)
    {
        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        return View(mapeador.Map<ExcluirProvaViewModel>(resultado.Value));
    }

    [HttpPost]
    public ActionResult Excluir(ExcluirProvaViewModel viewModel)
    {
        Result resultado = servicoProva.Excluir(viewModel.Id);

        if (resultado.IsFailed)
            TempData.AddErrorMessage(resultado);

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public JsonResult SelecionarMaterias(Guid disciplinaId, int serie)
    {
        return Json(servicoProva.SelecionarMaterias(disciplinaId, serie));
    }

    private EstadoGeracao? LerEstadoGeracao(Guid fluxoId, out bool invalido)
    {
        invalido = false;
        string key = ObterEstadoGeracaoKey(fluxoId);

        if (fluxoId == Guid.Empty || TempData.Peek(key) is not string serializado)
            return null;

        try
        {
            EstadoGeracao? estado = JsonSerializer.Deserialize<EstadoGeracao>(serializado);

            if (estado is not null)
                return estado;
        }
        catch (JsonException)
        {
            // O valor inválido é removido abaixo para não manter o fluxo corrompido.
        }

        invalido = true;
        TempData.Remove(key);
        return null;
    }

    private ConfiguracaoProva? LerConfiguracao(Guid fluxoId, out bool invalida)
    {
        invalida = false;
        string key = ObterConfiguracaoKey(fluxoId);

        if (fluxoId == Guid.Empty || TempData.Peek(key) is not string serializado)
            return null;

        try
        {
            ConfiguracaoProva? configuracao = JsonSerializer.Deserialize<ConfiguracaoProva>(serializado);

            if (configuracao is not null && ConfiguracaoEhValida(configuracao))
                return configuracao;
        }
        catch (JsonException)
        {
            // O valor inválido é removido abaixo para não manter o fluxo corrompido.
        }

        invalida = true;
        TempData.Remove(key);
        return null;
    }

    private string? ObterNomeDisciplina(Guid id)
    {
        return servicoProva.SelecionarDisciplinas().SingleOrDefault(d => d.Id == id)?.Nome;
    }

    private Result<ConfirmarProvaViewModel> MontarPrevia(EstadoGeracao estado)
    {
        if (!EstadoEhValido(estado))
            return Result.Fail<ConfirmarProvaViewModel>(new Error(MensagemEstadoInvalido)
                .WithMetadata("Campo", nameof(CadastrarProvaDto.QuantidadeQuestoes)));

        CadastrarProvaDto dto = new(
            estado.Titulo,
            estado.DisciplinaId,
            estado.MateriaId,
            estado.Serie,
            estado.QuantidadeQuestoes,
            estado.ProvaRecuperacao
        );

        Result<List<QuestaoProvaDto>> resultadoQuestoes = servicoProva
            .SelecionarQuestoes(dto, estado.QuestaoIds);

        if (resultadoQuestoes.IsFailed)
            return Result.Fail<ConfirmarProvaViewModel>(resultadoQuestoes.Errors);

        string? nomeDisciplina = ObterNomeDisciplina(estado.DisciplinaId);
        if (nomeDisciplina is null)
            return Result.Fail<ConfirmarProvaViewModel>(new Error("A disciplina selecionada não foi encontrada.")
                .WithMetadata("Campo", nameof(CadastrarProvaDto.DisciplinaId)));

        string? nomeMateria = estado.MateriaId.HasValue
            ? servicoProva
                .SelecionarMaterias(estado.DisciplinaId, estado.Serie)
                .SingleOrDefault(m => m.Id == estado.MateriaId.Value)?.Nome
            : null;

        if (!estado.ProvaRecuperacao && nomeMateria is null)
            return Result.Fail<ConfirmarProvaViewModel>(new Error("A matéria selecionada não foi encontrada.")
                .WithMetadata("Campo", nameof(CadastrarProvaDto.MateriaId)));

        return Result.Ok(new ConfirmarProvaViewModel(
            estado.Titulo,
            nomeDisciplina,
            nomeMateria,
            estado.Serie,
            estado.QuantidadeQuestoes,
            estado.ProvaRecuperacao,
            mapeador.Map<List<QuestaoProvaViewModel>>(resultadoQuestoes.Value)
        ));
    }

    private ActionResult RedirecionarAposFalhaNaPrevia(ResultBase resultado, Guid fluxoId)
    {
        string? campo = resultado.Errors
            .Select(erro => erro.Metadata.TryGetValue("Campo", out object? valor) ? valor?.ToString() : null)
            .FirstOrDefault(valor => valor is not null);

        string mensagem = resultado.Errors.First().Message;

        return campo is nameof(CadastrarProvaDto.Titulo) or nameof(CadastrarProvaDto.DisciplinaId)
            ? RedirecionarParaCadastro(mensagem, fluxoId)
            : RedirecionarParaSelecao(mensagem, fluxoId);
    }

    private RedirectToActionResult RedirecionarParaCadastro(string mensagem, Guid fluxoId)
    {
        RemoverFluxo(fluxoId);
        TempData["MensagemErro"] = mensagem;
        return RedirectToAction(nameof(Cadastrar));
    }

    private RedirectToActionResult RedirecionarParaSelecao(string mensagem, Guid fluxoId)
    {
        TempData["MensagemErro"] = mensagem;
        return RedirectToAction(nameof(SelecionarQuestoes), new { fluxoId });
    }

    private void RemoverFluxo(Guid fluxoId)
    {
        TempData.Remove(ObterConfiguracaoKey(fluxoId));
        TempData.Remove(ObterEstadoGeracaoKey(fluxoId));
    }

    private void PreservarFluxosAtivos()
    {
        string[] keys = [.. TempData.Keys.Where(key =>
            key.StartsWith(ConfiguracaoKeyPrefix, StringComparison.Ordinal)
            || key.StartsWith(EstadoGeracaoKeyPrefix, StringComparison.Ordinal))];

        foreach (string key in keys)
            TempData.Keep(key);
    }

    private static bool ConfiguracaoEhValida(ConfiguracaoProva configuracao)
    {
        return configuracao.DisciplinaId != Guid.Empty
            && configuracao.Serie > 0
            && !string.IsNullOrWhiteSpace(configuracao.Titulo)
            && configuracao.Titulo.Length <= 100;
    }

    private static bool EstadoEhValido(EstadoGeracao estado)
    {
        return estado.DisciplinaId != Guid.Empty
            && estado.Serie > 0
            && !string.IsNullOrWhiteSpace(estado.Titulo)
            && estado.Titulo.Length <= 100
            && estado.QuantidadeQuestoes is >= 1 and <= Prova.QuantidadeMaximaQuestoes
            && estado.QuestaoIds is not null
            && estado.QuestaoIds.Count == estado.QuantidadeQuestoes
            && estado.QuestaoIds.All(id => id != Guid.Empty)
            && estado.QuestaoIds.Distinct().Count() == estado.QuestaoIds.Count
            && (estado.ProvaRecuperacao ? !estado.MateriaId.HasValue : estado.MateriaId.HasValue);
    }

    private static string ObterConfiguracaoKey(Guid fluxoId) =>
        $"{ConfiguracaoKeyPrefix}{fluxoId:N}";

    private static string ObterEstadoGeracaoKey(Guid fluxoId) =>
        $"{EstadoGeracaoKeyPrefix}{fluxoId:N}";

    private ActionResult GerarPdf(Guid id, bool incluirGabarito, string prefixoArquivo)
    {
        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        byte[] pdf = GeradorPdf.Gerar(resultado.Value, incluirGabarito);
        string titulo = NormalizarNomeArquivo(resultado.Value.Titulo);
        string nomeArquivo = $"{prefixoArquivo}-{titulo}-{id:N}.pdf";

        return File(pdf, "application/pdf", nomeArquivo);
    }

    private static string NormalizarNomeArquivo(string titulo)
    {
        string normalizado = titulo.Normalize(NormalizationForm.FormD);
        string semAcentos = new([.. normalizado.Where(caracter =>
            CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
        ]);
        string slug = Regex.Replace(semAcentos.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "prova" : slug;
    }

    private void CarregarDisciplinas()
    {
        ViewBag.Disciplinas = servicoProva
            .SelecionarDisciplinas()
            .Select(d => new SelectListItem(d.Nome, d.Id.ToString()))
            .ToList();
    }

    private void CarregarMaterias(Guid? disciplinaId, int? serie)
    {
        ViewBag.Materias = disciplinaId.HasValue && serie.HasValue
            ? servicoProva
                .SelecionarMaterias(disciplinaId.Value, serie.Value)
                .Select(m => new SelectListItem(m.Nome, m.Id.ToString()))
                .ToList()
            : [];
    }
}
