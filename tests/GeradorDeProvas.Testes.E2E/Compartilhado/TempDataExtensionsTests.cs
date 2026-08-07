using FluentResults;
using GeradorDeProvas.WebApp.Compartilhado.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
public sealed class TempDataExtensionsTests
{
    [TestMethod]
    public void AddErrorMessage_ResultadoComErro_ArmazenaPrimeiraMensagem()
    {
        var tempData = new TempDataDictionary(new DefaultHttpContext(), new ProvedorTemporario());
        Result resultado = Result.Fail([new Error("Primeiro erro."), new Error("Segundo erro.")]);

        tempData.AddErrorMessage(resultado);

        Assert.AreEqual("Primeiro erro.", tempData["MensagemErro"]);
    }

    private sealed class ProvedorTemporario : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
