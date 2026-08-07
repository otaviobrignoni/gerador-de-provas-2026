using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaFormPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/Cadastrar";

    public ILocator Titulo => page.GetByLabel("Título");
    public ILocator Disciplina => page.GetByLabel("Disciplina");
    public ILocator Serie => page.GetByLabel("Série");
    public ILocator ProvaRecuperacao => page.GetByLabel("Prova de recuperação (todas as matérias)");

    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }

    public async Task PreencherAsync(string titulo, string disciplina, int serie)
    {
        await Titulo.FillAsync(titulo);
        await Disciplina.SelectOptionAsync(new SelectOptionValue { Label = disciplina });
        await Serie.FillAsync(serie.ToString());
    }

    public async Task ContinuarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Continuar", Exact = true }).ClickAsync();
    }

    public async Task MarcarComoRecuperacaoAsync()
    {
        await ProvaRecuperacao.CheckAsync();
    }
}
