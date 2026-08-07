using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaSelecionarQuestoesPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/SelecionarQuestoes";

    public ILocator Materia => page.GetByLabel("Matéria");
    public ILocator QuantidadeQuestoes => page.GetByLabel("Quantidade de questões para sortear");

    public async Task PreencherAsync(string materia, int quantidadeQuestoes)
    {
        await Materia.SelectOptionAsync(new SelectOptionValue { Label = materia });
        await QuantidadeQuestoes.FillAsync(quantidadeQuestoes.ToString());
    }

    public async Task SortearAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Sortear questões", Exact = true }).ClickAsync();
    }
}
