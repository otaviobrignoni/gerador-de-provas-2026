using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

public sealed class DisciplinaListarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Disciplina/Listar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Disciplinas" });

    public ILocator CadastarNova => page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" });

    public ILocator EstadoVazio => page.GetByText("Nenhuma disciplina cadastrada.", new() { Exact = true });

    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }
}
