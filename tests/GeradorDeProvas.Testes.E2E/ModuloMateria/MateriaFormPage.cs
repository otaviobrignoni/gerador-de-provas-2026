using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloMateria;

public sealed class MateriaFormPage(IPage page, string urlBase)
{
    public string UrlCadastrar => $"{urlBase}/Materia/Cadastrar";
    public string UrlEditar => $"{urlBase}/Materia/Editar";

    public ILocator Nome => page.GetByLabel("Nome");
    public ILocator Disciplina => page.GetByLabel("Disciplina");
    public ILocator Serie => page.GetByLabel("Série");

    public async Task IrParaCadastroAsync()
    {
        await page.GotoAsync(UrlCadastrar);
    }

    public async Task IrParaEdicaoAsync()
    {
        await page.GotoAsync(UrlEditar);
    }

    public async Task PreencherAsync(string nome, string disciplina, int serie)
    {
        await Nome.FillAsync(nome);
        await Disciplina.SelectOptionAsync(new SelectOptionValue { Label = disciplina });
        await Serie.FillAsync(serie.ToString());
    }

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }
}
