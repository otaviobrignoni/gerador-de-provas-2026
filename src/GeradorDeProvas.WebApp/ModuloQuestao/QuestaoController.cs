using AutoMapper;
using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloQuestao;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GeradorDeProvas.WebApp.ModuloQuestao;

public class QuestaoController(
    ServicoQuestao servicoQuestao,
    IMapper mapeador
) : Controller
{
    [HttpGet]
    public ActionResult Listar()
    {
        List<ListarQuestaoDto> dtos = servicoQuestao.SelecionarTodos();

        List<ListarQuestaoViewModel> listarVms = mapeador.Map<List<ListarQuestaoViewModel>>(dtos);

        return View(listarVms);
    }

    [HttpGet]
    public ActionResult Cadastrar()
    {
        CadastrarQuestaoViewModel cadastrarVm = new(
            string.Empty,
            null,
            [new(string.Empty, false), new(string.Empty, false)]
        );

        CarregarMaterias();

        return View(cadastrarVm);
    }

    [HttpPost]
    public ActionResult Cadastrar(CadastrarQuestaoViewModel cadastrarVm)
    {
        if (!ModelState.IsValid)
        {
            CarregarMaterias();
            return View(cadastrarVm);
        }

        CadastrarQuestaoDto dto = mapeador.Map<CadastrarQuestaoDto>(cadastrarVm);

        Result resultado = servicoQuestao.Cadastrar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            CarregarMaterias();
            return View(cadastrarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Editar(Guid id)
    {
        Result<DetalhesQuestaoDto> resultado = servicoQuestao.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        EditarQuestaoViewModel editarVm = mapeador.Map<EditarQuestaoViewModel>(resultado.Value);

        CarregarMaterias();

        return View(editarVm);
    }

    [HttpPost]
    public ActionResult Editar(EditarQuestaoViewModel editarVm)
    {
        if (!ModelState.IsValid)
        {
            CarregarMaterias();
            return View(editarVm);
        }

        EditarQuestaoDto dto = mapeador.Map<EditarQuestaoDto>(editarVm);

        Result resultado = servicoQuestao.Editar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            CarregarMaterias();
            return View(editarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Excluir(Guid id)
    {
        Result<DetalhesQuestaoDto> resultado = servicoQuestao.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        ExcluirQuestaoViewModel excluirVm = mapeador.Map<ExcluirQuestaoViewModel>(resultado.Value);

        return View(excluirVm);
    }

    [HttpPost]
    public ActionResult Excluir(ExcluirQuestaoViewModel excluirVm)
    {
        Result resultado = servicoQuestao.Excluir(excluirVm.Id);

        if (resultado.IsFailed)
            TempData.AddErrorMessage(resultado);

        return RedirectToAction(nameof(Listar));
    }

    private void CarregarMaterias()
    {
        List<OpcaoMateriaQuestaoDto> materias = servicoQuestao.SelecionarMaterias();

        ViewBag.Materias = materias
            .Select(m => new SelectListItem(m.Nome, m.Id.ToString()))
            .ToList();
    }
}
