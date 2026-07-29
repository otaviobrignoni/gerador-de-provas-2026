using AutoMapper;
using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloMateria;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GeradorDeProvas.WebApp.ModuloMateria;

public class MateriaController(
    ServicoMateria servicoMateria,
    IMapper mapeador
) : Controller
{
    [HttpGet]
    public ActionResult Listar()
    {
        List<ListarMateriaDto> dtos = servicoMateria.SelecionarTodos();

        List<ListarMateriaViewModel> listarVms = mapeador.Map<List<ListarMateriaViewModel>>(dtos);

        return View(listarVms);
    }

    [HttpGet]
    public ActionResult Cadastrar()
    {
        CadastrarMateriaViewModel cadastrarVm = new(string.Empty, null, null);

        CarregarDisciplinas();

        return View(cadastrarVm);
    }

    [HttpPost]
    public ActionResult Cadastrar(CadastrarMateriaViewModel cadastrarVm)
    {
        if (!ModelState.IsValid)
        {
            CarregarDisciplinas();
            return View(cadastrarVm);
        }

        CadastrarMateriaDto dto = mapeador.Map<CadastrarMateriaDto>(cadastrarVm);

        Result resultado = servicoMateria.Cadastrar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            CarregarDisciplinas();
            return View(cadastrarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Editar(Guid id)
    {
        Result<DetalhesMateriaDto> resultado = servicoMateria.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        EditarMateriaViewModel editarVm = mapeador.Map<EditarMateriaViewModel>(resultado.Value);

        CarregarDisciplinas();

        return View(editarVm);
    }

    [HttpPost]
    public ActionResult Editar(EditarMateriaViewModel editarVm)
    {
        if (!ModelState.IsValid)
        {
            CarregarDisciplinas();
            return View(editarVm);
        }

        EditarMateriaDto dto = mapeador.Map<EditarMateriaDto>(editarVm);

        Result resultado = servicoMateria.Editar(dto);

        if (resultado.IsFailed)
        {
            ModelState.AddModelError(resultado);
            CarregarDisciplinas();
            return View(editarVm);
        }

        return RedirectToAction(nameof(Listar));
    }

    [HttpGet]
    public ActionResult Excluir(Guid id)
    {
        Result<DetalhesMateriaDto> resultado = servicoMateria.SelecionarPorId(id);

        if (resultado.IsFailed)
        {
            TempData.AddErrorMessage(resultado);
            return RedirectToAction(nameof(Listar));
        }

        ExcluirMateriaViewModel excluirVm = mapeador.Map<ExcluirMateriaViewModel>(resultado.Value);

        return View(excluirVm);
    }

    [HttpPost]
    public ActionResult Excluir(ExcluirMateriaViewModel excluirVm)
    {
        Result resultado = servicoMateria.Excluir(excluirVm.Id);

        if (resultado.IsFailed)
            TempData.AddErrorMessage(resultado);

        return RedirectToAction(nameof(Listar));
    }

    private void CarregarDisciplinas()
    {
        List<OpcaoDisciplinaMateriaDto> disciplinas = servicoMateria.SelecionarDisciplinas();

        ViewBag.Disciplinas = disciplinas
            .Select(d => new SelectListItem(d.Nome, d.Id.ToString()))
            .ToList();
    }
}
