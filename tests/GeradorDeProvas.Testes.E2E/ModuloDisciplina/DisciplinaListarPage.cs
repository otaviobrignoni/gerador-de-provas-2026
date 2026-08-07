using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloDisciplina;

public sealed class DisciplinaListarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Disciplina/Listar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Disciplinas" });

    public ILocator CadastarNova => page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" });

    public ILocator EstadoVazio => page.GetByText("Nenhuma disciplina cadastrada.", new() { Exact = true });

    public ILocator NomeDaDisciplina(string nome) => page.GetByRole(AriaRole.Heading, new() { Name = nome, Exact = true });
    public ILocator MensagemErro(string mensagem) => page.GetByRole(AriaRole.Alert).GetByText(mensagem, new() { Exact = true });

    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }
    public async Task EditarAsync(string nome)
    {
        await CardPorNome(nome).GetByRole(AriaRole.Link, new() { Name = "Editar", Exact = true }).ClickAsync();
    }

    public async Task ExcluirAsync(string nome)
    {
        await CardPorNome(nome).GetByRole(AriaRole.Link, new() { Name = "Excluir", Exact = true }).ClickAsync();
    }

    private ILocator CardPorNome(string nome)
    {
        return page.Locator(".card").Filter(new() { Has = NomeDaDisciplina(nome) });
    }
}
