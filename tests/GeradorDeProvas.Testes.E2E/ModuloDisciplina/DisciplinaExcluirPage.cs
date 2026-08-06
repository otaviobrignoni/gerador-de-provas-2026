using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

public sealed class DisciplinaExcluirPage(IPage page)
{
    public ILocator MensagemConfirmacao => page.GetByText("Deseja realmente excluir esta disciplina?", new() { Exact = true });

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }
}
