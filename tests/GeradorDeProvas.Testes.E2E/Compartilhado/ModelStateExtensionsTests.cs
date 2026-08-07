using FluentResults;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
public sealed class ModelStateExtensionsTests
{
    [TestMethod]
    public void AddModelError_ErroComCampo_AdicionaErroAoCampoInformado()
    {
        var modelState = new ModelStateDictionary();
        Result resultado = Result.Fail(new Error("Título inválido.").WithMetadata("Campo", "Titulo"));

        modelState.AddModelError(resultado);

        Assert.AreEqual("Título inválido.", modelState["Titulo"]!.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    public void AddModelError_ErroSemCampo_AdicionaErroAoModelo()
    {
        var modelState = new ModelStateDictionary();
        Result resultado = Result.Fail("Não há questões suficientes.");

        modelState.AddModelError(resultado);

        Assert.AreEqual("Não há questões suficientes.", modelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    public void AddModelError_MultiplosErros_AdicionaTodos()
    {
        var modelState = new ModelStateDictionary();
        Result resultado = Result.Fail([new Error("Primeiro erro."), new Error("Segundo erro.").WithMetadata("Campo", "Nome")]);

        modelState.AddModelError(resultado);

        Assert.AreEqual("Primeiro erro.", modelState[string.Empty]!.Errors.Single().ErrorMessage);
        Assert.AreEqual("Segundo erro.", modelState["Nome"]!.Errors.Single().ErrorMessage);
    }
}
