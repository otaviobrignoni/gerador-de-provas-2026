using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaExcluirPage(IPage page, string urlBase)
{
    public string UrlBase => $"{urlBase}/Prova/Excluir";
    public ILocator MensagemConfirmacao => page.GetByText("Deseja realmente excluir esta prova?", new() { Exact = true });

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }
}
