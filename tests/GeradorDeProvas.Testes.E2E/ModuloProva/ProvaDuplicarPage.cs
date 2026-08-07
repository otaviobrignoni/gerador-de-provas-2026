using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaDuplicarPage(IPage page, string urlBase)
{
    public string UrlBase => $"{urlBase}/Prova/Duplicar";
    public ILocator NovoTitulo => page.GetByLabel("Novo título");

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Duplicar", Exact = true }).ClickAsync();
    }
}
