using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloAutenticacao;

public sealed class EntrarPage(IPage page, string urlBase)
{
    public string Url => $"{urlBase}/Autenticacao/Entrar";

    public ILocator Titulo => page.GetByRole(AriaRole.Heading, new() { Name = "Entrar", Exact = true });
    public ILocator Email => page.GetByLabel("E-mail");
    public ILocator Senha => page.GetByLabel("Senha", new() { Exact = true });
    public ILocator MensagemCredenciaisInvalidas => page.GetByText("E-mail ou senha inválidos.", new() { Exact = true });
    public ILocator MensagemContaBloqueada => page.GetByText("Conta bloqueada temporariamente. Tente novamente mais tarde.", new() { Exact = true });

    public ILocator UsuarioAutenticado(string email) => page.GetByRole(AriaRole.Button, new() { Name = email, Exact = true });

    public async Task IrParaAsync()
    {
        await page.GotoAsync(Url);
    }

    public async Task PreencherAsync(string email, string senha)
    {
        await Email.FillAsync(email);
        await Senha.FillAsync(senha);
    }

    public async Task ConfirmarAsync()
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Entrar", Exact = true }).ClickAsync();
    }

}
