using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaConfirmarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/Confirmar";

    public ILocator TituloDaProva(string titulo) => page.GetByText(titulo, new() { Exact = true });

    public ILocator QuestaoSorteada(string enunciado) => page.GetByText(enunciado);

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar geração", Exact = true }).ClickAsync();
    }
}
