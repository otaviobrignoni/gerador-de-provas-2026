using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloMateria;

public sealed class MateriaExcluirPage(IPage page)
{
    public ILocator MensagemConfirmacao => page.GetByText("Deseja realmente excluir esta matéria?", new() { Exact = true });

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }
}
