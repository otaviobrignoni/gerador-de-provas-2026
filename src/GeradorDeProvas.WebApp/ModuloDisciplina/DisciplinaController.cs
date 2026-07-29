using AutoMapper;
using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloDisciplina;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace GeradorDeProvas.WebApp.ModuloDisciplina;

public class DisciplinaController(
    ServicoDisciplina servicoDisciplina,
    IMapper mapeador
) : Controller
{
    [HttpGet]
    public ActionResult Listar()
    {
        List<ListarDisciplinaDto> dtos = servicoDisciplina.SelecionarTodos();

        List<ListarDisciplinaViewModel> listarVms = mapeador.Map<List<ListarDisciplinaViewModel>>(dtos);

        return View(listarVms);
    }

    [HttpGet]
    public ActionResult Cadastrar()
    {
        CadastrarDisciplinaViewModel cadastrarVm = new(string.Empty);

        return View(cadastrarVm);
    }

    [HttpPost]
    public ActionResult Cadastrar(CadastrarDisciplinaViewModel cadastrarVm)
    {
        if (!ModelState.IsValid)
            return View(cadastrarVm);

        CadastrarDisciplinaDto dto = mapeador.Map<CadastrarDisciplinaDto>(cadastrarVm);

        Result resultado = servicoDisciplina.Cadastrar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);

            return View(cadastrarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Editar(Guid id)
    {
        Result<DetalhesDisciplinaDto> resultado = servicoDisciplina.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);

            return RedirectToAction(nameof(Listar));
        }

        EditarDisciplinaViewModel editarVm = mapeador.Map<EditarDisciplinaViewModel>(resultado.Value);

        return View(editarVm);
    }

    [HttpPost]
    public ActionResult Editar(EditarDisciplinaViewModel editarVm)
    {
        if (!ModelState.IsValid)
            return View(editarVm);

        EditarDisciplinaDto dto = mapeador.Map<EditarDisciplinaDto>(editarVm);

        Result resultado = servicoDisciplina.Editar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);

            return View(editarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Excluir(Guid id)
    {
        Result<DetalhesDisciplinaDto> resultado = servicoDisciplina.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);

            return RedirectToAction(nameof(Listar));
        }

        ExcluirDisciplinaViewModel excluirVm = mapeador.Map<ExcluirDisciplinaViewModel>(resultado.Value);

        return View(excluirVm);
    }

    [HttpPost]
    public ActionResult Excluir(ExcluirDisciplinaViewModel excluirVm)
    {
        Result resultado = servicoDisciplina.Excluir(excluirVm.Id);

        if (resultado.IsFailed)
            TempData.AddErrorMessage(resultado);

        return RedirectToAction(nameof(Listar));
    }
}
