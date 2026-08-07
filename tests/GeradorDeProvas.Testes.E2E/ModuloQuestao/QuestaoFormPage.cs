using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloQuestao;

public sealed class QuestaoFormPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Questao/Cadastrar";

    public ILocator Enunciado => page.GetByLabel("Enunciado");
    public ILocator Materia => page.GetByLabel("Matéria");
    public ILocator Alternativas => page.Locator("#alternativas .alternative-item");
    public ILocator MensagemErro(string mensagem) => page.GetByText(mensagem, new() { Exact = true });


    public async Task IrParaCadastroAsync()
    {
        await page.GotoAsync(Url);
    }

    public async Task PreencherAsync(string enunciado, string materia, string[] alternativas, int indiceCorreta)
    {
        await Enunciado.FillAsync(enunciado);
        await Materia.SelectOptionAsync(new SelectOptionValue { Label = materia });

        int contagemAlternativas = await Alternativas.CountAsync();

        while (contagemAlternativas < alternativas.Length)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Adicionar alternativa", Exact = true }).ClickAsync();
            contagemAlternativas = await Alternativas.CountAsync();
        }

        while (contagemAlternativas > alternativas.Length)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Remover", Exact = true }).Last.ClickAsync();
            contagemAlternativas = await Alternativas.CountAsync();
        }

        for (int indice = 0; indice < alternativas.Length; indice++)
        {
            await page.Locator($"input[name='Alternativas[{indice}].Texto']").FillAsync(alternativas[indice]);

            if (await page.Locator($"input[name='Alternativas[{indice}].Correta']").IsCheckedAsync())
                await page.Locator($"input[name='Alternativas[{indice}].Correta']").UncheckAsync();

            if (indice == indiceCorreta)
                await page.Locator($"input[name='Alternativas[{indice}].Correta']").CheckAsync();
        }
    }

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Confirmar", Exact = true }).ClickAsync();
    }

    public async Task MarcarAlternativaCorretaAsync(int indice)
    {
        await page.Locator($"input[name='Alternativas[{indice}].Correta']").CheckAsync();
    }
}
