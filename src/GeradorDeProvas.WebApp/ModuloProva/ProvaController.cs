using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloProva;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GeradorDeProvas.WebApp.ModuloProva;


public class ProvaController(ServicoProva servicoProva, IMapper mapeador) : Controller
{
    private const string ConfiguracaoKey = "Prova.Configuracao";
    private const string EstadoGeracaoKey = "Prova.EstadoGeracao";

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
        CadastrarProvaEtapa1ViewModel viewModel = new(string.Empty, null, null, false);
        CarregarDisciplinas();
        return View(viewModel);
    }

    [HttpPost]
    public ActionResult Cadastrar(CadastrarProvaEtapa1ViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            CarregarDisciplinas();
            return View(viewModel);
        }

        TempData[ConfiguracaoKey] = JsonSerializer.Serialize(new ConfiguracaoProva(
            viewModel.Titulo,
            viewModel.DisciplinaId!.Value,
            viewModel.Serie!.Value,
            viewModel.ProvaRecuperacao
        ));

        return RedirectToAction(nameof(SelecionarQuestoes));
    }

    [HttpGet]
    public ActionResult SelecionarQuestoes()
    {
        ConfiguracaoProva? configuracao = LerConfiguracao();
        EstadoGeracao? estadoAnterior = configuracao is null ? LerEstadoGeracao() : null;

        if (configuracao is null && estadoAnterior is not null)
        {
            configuracao = new ConfiguracaoProva(
                estadoAnterior.Titulo,
                estadoAnterior.DisciplinaId,
                estadoAnterior.Serie,
                estadoAnterior.ProvaRecuperacao
            );
        }

        if (configuracao is null)
            return RedirectToAction(nameof(Cadastrar));

        string? nomeDisciplina = ObterNomeDisciplina(configuracao.DisciplinaId);

        if (nomeDisciplina is null)
        {
            TempData["MensagemErro"] = "A disciplina selecionada não foi encontrada.";
            return RedirectToAction(nameof(Cadastrar));
        }

        CarregarMaterias(configuracao.DisciplinaId, configuracao.Serie);

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
    public ActionResult SelecionarQuestoes(CadastrarProvaEtapa2ViewModel viewModel)
    {
        ConfiguracaoProva? configuracao = LerConfiguracao();
        EstadoGeracao? estadoAnterior = configuracao is null ? LerEstadoGeracao() : null;

        if (configuracao is null && estadoAnterior is not null)
        {
            configuracao = new ConfiguracaoProva(
                estadoAnterior.Titulo,
                estadoAnterior.DisciplinaId,
                estadoAnterior.Serie,
                estadoAnterior.ProvaRecuperacao
            );
        }

        if (configuracao is null)
            return RedirectToAction(nameof(Cadastrar));

        string? nomeDisciplina = ObterNomeDisciplina(configuracao.DisciplinaId);

        if (nomeDisciplina is null)
            return RedirectToAction(nameof(Cadastrar));

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
            return View(modeloAtual);
        }

        TempData.Remove(ConfiguracaoKey);

        TempData[EstadoGeracaoKey] = JsonSerializer.Serialize(new EstadoGeracao(
            configuracao.Titulo,
            configuracao.DisciplinaId,
            viewModel.MateriaId,
            configuracao.Serie,
            viewModel.QuantidadeQuestoes.Value,
            configuracao.ProvaRecuperacao,
            resultado.Value.Select(q => q.Id).ToList()
        ));

        return RedirectToAction(nameof(Confirmar));
    }

    [HttpGet]
    public ActionResult Confirmar()
    {
        EstadoGeracao? estado = LerEstadoGeracao();

        if (estado is null)
            return RedirectToAction(nameof(Cadastrar));

        Result<ConfirmarProvaViewModel> resultado = MontarPrevia(estado);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Cadastrar));
        }

        return View(resultado.Value);
    }

    [HttpPost]
    public ActionResult Confirmar(IFormCollection _)
    {
        EstadoGeracao? estado = LerEstadoGeracao();

        if (estado is null)
            return RedirectToAction(nameof(Cadastrar));

        CadastrarProvaDto dto = new(
            estado.Titulo,
            estado.DisciplinaId,
            estado.MateriaId,
            estado.Serie,
            estado.QuantidadeQuestoes,
            estado.ProvaRecuperacao
        );

        Result resultado = servicoProva.Cadastrar(dto, estado.QuestaoIds);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);

            Result<ConfirmarProvaViewModel> previa = MontarPrevia(estado);

            if (previa.IsFailed)
                return RedirectToAction(nameof(Cadastrar));

            return View(previa.Value);
        }

        TempData.Remove(EstadoGeracaoKey);

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

    private EstadoGeracao? LerEstadoGeracao()
    {
        if (TempData.Peek(EstadoGeracaoKey) is not string serializado)
            return null;

        try
        {
            return JsonSerializer.Deserialize<EstadoGeracao>(serializado);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private ConfiguracaoProva? LerConfiguracao()
    {
        if (TempData.Peek(ConfiguracaoKey) is not string serializado)
            return null;

        try
        {
            return JsonSerializer.Deserialize<ConfiguracaoProva>(serializado);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string? ObterNomeDisciplina(Guid id)
    {
        return servicoProva.SelecionarDisciplinas().SingleOrDefault(d => d.Id == id)?.Nome;
    }

    private Result<ConfirmarProvaViewModel> MontarPrevia(EstadoGeracao estado)
    {
        Result<List<QuestaoProvaDto>> resultadoQuestoes = servicoProva.SelecionarQuestoes(estado.QuestaoIds);

        if (resultadoQuestoes.IsFailed)
            return Result.Fail<ConfirmarProvaViewModel>(resultadoQuestoes.Errors);

        string? nomeDisciplina = ObterNomeDisciplina(estado.DisciplinaId);
        if (nomeDisciplina is null)
            return Result.Fail<ConfirmarProvaViewModel>("A disciplina selecionada não foi encontrada.");

        string? nomeMateria = estado.MateriaId.HasValue
            ? servicoProva
                .SelecionarMaterias(estado.DisciplinaId, estado.Serie)
                .SingleOrDefault(m => m.Id == estado.MateriaId.Value)?.Nome
            : null;

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
