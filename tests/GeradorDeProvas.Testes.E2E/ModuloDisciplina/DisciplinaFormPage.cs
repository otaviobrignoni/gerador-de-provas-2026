using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

public sealed class DisciplinaFormPage(IPage page, string urlBase)
{
    public string UrlCadastrar => $"{urlBase}/Disciplina/Cadastrar";
    public string UrlEditar => $"{urlBase}/Disciplina/Editar";

    public async Task IrParaCadastroAsync()
    {
        await page.GotoAsync(UrlCadastrar);
    }

    public async Task IrParaEdicaoAsync()
    {
        await page.GotoAsync(UrlEditar);
    }

    public async Task PreencherNomeAsync(string nome)
    {
        await page.GetByLabel("Nome").FillAsync(nome);
    }

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar" }).ClickAsync();
    }
}
