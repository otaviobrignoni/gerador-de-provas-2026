using GeradorDeProvas.Aplicacao.Compartilhado;

namespace GeradorDeProvas.Testes.Unidade.Compartilhado;

[TestClass]
public sealed class StringExtensionsTests
{
    [TestMethod]
    public void Normalizar_TextoComEspacosECaixaMista_RemoveEspacosEConverteParaMinusculas()
    {
        string resultado = "  MaTeMáTiCa  ".Normalizar();

        Assert.AreEqual("matemática", resultado);
    }

    [TestMethod]
    public void Normalizar_TextoVazio_RetornaTextoVazio()
    {
        Assert.AreEqual(string.Empty, "   ".Normalizar());
    }
}
