using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaConfirmarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/Confirmar";
    public Regex UrlComFluxo => new($"^{Regex.Escape(Url)}\\?fluxoId=[0-9a-fA-F-]{{36}}$");

    public ILocator TituloDaProva(string titulo) => page.GetByText(titulo, new() { Exact = true });

    public ILocator QuestaoSorteada(string enunciado) => page.GetByText(enunciado);

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar geração", Exact = true }).ClickAsync();
    }
}
