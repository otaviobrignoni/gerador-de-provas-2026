using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaListarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/Listar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Provas" });

    public ILocator GerarNova => page.GetByRole(AriaRole.Link, new() { Name = "Gerar Nova", Exact = true });

    public ILocator EstadoVazio => page.GetByText("Nenhuma prova cadastrada.", new() { Exact = true });

    public ILocator TituloDaProva(string titulo) => page.GetByRole(AriaRole.Heading, new() { Name = titulo, Exact = true });

    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }

    public async Task<IDownload> BaixarPdfAsync(string titulo)
    {
        return await page.RunAndWaitForDownloadAsync(async () => await CardPorTitulo(titulo).GetByRole(AriaRole.Link, new() { Name = "PDF", Exact = true }).ClickAsync());
    }

    public async Task<IDownload> BaixarGabaritoAsync(string titulo)
    {
        return await page.RunAndWaitForDownloadAsync(async () => await CardPorTitulo(titulo).GetByRole(AriaRole.Link, new() { Name = "Gabarito", Exact = true }).ClickAsync());
    }

    public async Task VerDetalhesAsync(string titulo)
    {
        await CardPorTitulo(titulo).GetByRole(AriaRole.Link, new() { Name = "Detalhes", Exact = true }).ClickAsync();
    }

    public async Task DuplicarAsync(string titulo)
    {
        await CardPorTitulo(titulo).GetByRole(AriaRole.Link, new() { Name = "Duplicar", Exact = true }).ClickAsync();
    }

    public async Task ExcluirAsync(string titulo)
    {
        await CardPorTitulo(titulo).GetByRole(AriaRole.Link, new() { Name = "Excluir", Exact = true }).ClickAsync();
    }

    private ILocator CardPorTitulo(string titulo)
    {
        return page.Locator(".card").Filter(new() { Has = TituloDaProva(titulo) });
    }
}
