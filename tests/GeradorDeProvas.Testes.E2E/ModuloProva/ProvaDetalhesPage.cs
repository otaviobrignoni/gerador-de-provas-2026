using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaDetalhesPage(IPage page, string urlBase)
{
    public string UrlBase => $"{urlBase}/Prova/Detalhes";

    public ILocator TituloDaProva(string titulo) => page.GetByText(titulo, new() { Exact = true });
    public ILocator Questao(string enunciado) => page.GetByText(new Regex($"^1\\. {Regex.Escape(enunciado)}$"));

    public async Task VoltarAsync()
    {
        await page.GetByRole(AriaRole.Link, new() { Name = "Voltar", Exact = true }).ClickAsync();
    }
}
