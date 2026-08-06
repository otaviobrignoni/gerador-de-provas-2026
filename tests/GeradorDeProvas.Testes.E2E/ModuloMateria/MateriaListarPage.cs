using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloMateria;

public sealed class MateriaListarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Materia/Listar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Listagem de Matérias" });

    public ILocator CadastarNova => page.GetByRole(AriaRole.Link, new() { Name = "Cadastrar Nova" });

    public ILocator EstadoVazio => page.GetByText("Nenhuma matéria cadastrada.", new() { Exact = true });

    public ILocator NomeDaMateria(string nome) => page.GetByRole(AriaRole.Heading, new() { Name = nome, Exact = true });

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
        ILocator nomeMateria = NomeDaMateria(nome);

        return page.Locator(".card").Filter(new() { Has = nomeMateria });
    }
}
