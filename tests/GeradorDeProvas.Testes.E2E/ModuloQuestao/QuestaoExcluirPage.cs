using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloQuestao;

public sealed class QuestaoExcluirPage(IPage page)
{
    public ILocator MensagemConfirmacao => page.GetByText("Deseja realmente excluir esta questão?", new() { Exact = true });

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }
}
