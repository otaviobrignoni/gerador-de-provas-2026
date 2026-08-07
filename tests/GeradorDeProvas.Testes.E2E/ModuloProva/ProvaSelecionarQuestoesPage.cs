using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

public sealed class ProvaSelecionarQuestoesPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Prova/SelecionarQuestoes";
    public Regex UrlComFluxo => new($"^{Regex.Escape(Url)}\\?fluxoId=[0-9a-fA-F-]{{36}}$");

    public ILocator Materia => page.GetByLabel("Matéria");
    public ILocator QuantidadeQuestoes => page.GetByLabel("Quantidade de questões para sortear");
    public ILocator AvisoRecuperacao => page.GetByText("Esta é uma prova de recuperação. O sorteio considerará todas as matérias da disciplina.", new() { Exact = true });
    public ILocator MensagemErro(string mensagem) => page.GetByText(mensagem, new() { Exact = true });

    public async Task PreencherAsync(string materia, int quantidadeQuestoes)
    {
        await Materia.SelectOptionAsync(new SelectOptionValue { Label = materia });
        await QuantidadeQuestoes.FillAsync(quantidadeQuestoes.ToString());
    }

    public async Task PreencherRecuperacaoAsync(int quantidadeQuestoes)
    {
        await QuantidadeQuestoes.FillAsync(quantidadeQuestoes.ToString());
    }

    public async Task SortearAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Sortear questões", Exact = true }).ClickAsync();
    }
}
