
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloQuestao;

public sealed class QuestaoListarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Questao/Listar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Questões" });

    public ILocator CadastrarNova => page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" });

    public ILocator EstadoVazio => page.GetByText("Nenhuma questão cadastrada.", new() { Exact = true });

    public ILocator EnunciadoDaQuestao(string enunciado) => page.GetByRole(AriaRole.Heading, new() { Name = enunciado, Exact = true });
    public ILocator MensagemErro(string mensagem) => page.GetByRole(AriaRole.Alert).GetByText(mensagem, new() { Exact = true });


    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }

    public async Task EditarAsync(string nome)
    {
        await CardPorEnunciado(nome).GetByRole(AriaRole.Link, new() { Name = "Editar", Exact = true }).ClickAsync();
    }

    public async Task ExcluirAsync(string nome)
    {
        await CardPorEnunciado(nome).GetByRole(AriaRole.Link, new() { Name = "Excluir", Exact = true }).ClickAsync();
    }

    private ILocator CardPorEnunciado(string enunciado)
    {
        return page.Locator(".card").Filter(new() { Has = EnunciadoDaQuestao(enunciado) });
    }
}
