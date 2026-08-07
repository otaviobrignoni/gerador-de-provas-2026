using System.Net;
using System.Net.Http.Headers;
using System.Text;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Testes.E2E.Compartilhado;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

[TestClass]
public sealed class ProvaPdfHttpTests
{
    [TestMethod]
    [TestCategory("HTTP")]
    public async Task EndpointsPdfEGabarito_RetornamArquivosValidosComNomeEConteudoEsperados()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        Guid userId = await RegistrarUsuarioAsync(factory, client);
        const string titulo = "  Avaliação: Espaços, Acentos & Símbolos?!  ";
        Prova prova = await CadastrarProvaAsync(factory, userId, titulo, recuperacao: false);

        using HttpResponseMessage responsePdf = await client.GetAsync($"/Prova/Pdf/{prova.Id}");
        using HttpResponseMessage responseGabarito = await client.GetAsync($"/Prova/Gabarito/{prova.Id}");
        byte[] pdf = await responsePdf.Content.ReadAsByteArrayAsync();
        byte[] gabarito = await responseGabarito.Content.ReadAsByteArrayAsync();

        Assert.AreEqual(HttpStatusCode.OK, responsePdf.StatusCode);
        Assert.AreEqual("application/pdf", responsePdf.Content.Headers.ContentType?.MediaType);
        Assert.IsGreaterThanOrEqualTo(4, pdf.Length);
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("%PDF"), pdf[..4]);
        Assert.AreEqual(HttpStatusCode.OK, responseGabarito.StatusCode);
        Assert.AreEqual("application/pdf", responseGabarito.Content.Headers.ContentType?.MediaType);
        Assert.IsGreaterThanOrEqualTo(4, gabarito.Length);
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("%PDF"), gabarito[..4]);

        string textoPdf = LerTexto(pdf);
        string textoGabarito = LerTexto(gabarito);

        Assert.AreEqual(
            $"prova-avaliacao-espacos-acentos-simbolos-{prova.Id:N}.pdf",
            ObterNomeArquivo(responsePdf.Content.Headers.ContentDisposition)
        );

        Assert.Contains("Avaliação: Espaços, Acentos & Símbolos?!", textoPdf);
        Assert.Contains("Disciplina: Ciências", textoPdf);
        Assert.Contains("Matéria: Astronomia", textoPdf);
        Assert.Contains("1. Qual planeta é conhecido como planeta vermelho?", textoPdf);
        Assert.Contains("[ ] Marte", textoPdf);
        Assert.Contains("[ ] Vênus", textoPdf);
        Assert.DoesNotContain("[X]", textoPdf);

        Assert.AreEqual(
            $"gabarito-avaliacao-espacos-acentos-simbolos-{prova.Id:N}.pdf",
            ObterNomeArquivo(responseGabarito.Content.Headers.ContentDisposition)
        );
        Assert.Contains("[X] Marte", textoGabarito);
        Assert.Contains("[ ] Vênus", textoGabarito);
        Assert.AreEqual(1, Regex.Matches(textoGabarito, "\\[X\\]").Count);
        Assert.DoesNotContain("[ ] Marte", textoGabarito);
        Assert.DoesNotContain("[X] Vênus", textoGabarito);
    }

    [TestMethod]
    [TestCategory("HTTP")]
    public async Task EndpointPdf_ProvaDeRecuperacao_NaoApresentaMateria()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        Guid userId = await RegistrarUsuarioAsync(factory, client);
        Prova prova = await CadastrarProvaAsync(
            factory,
            userId,
            "Recuperação de Ciências",
            recuperacao: true
        );

        using HttpResponseMessage response = await client.GetAsync($"/Prova/Pdf/{prova.Id}");
        byte[] pdf = await response.Content.ReadAsByteArrayAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.IsGreaterThanOrEqualTo(4, pdf.Length);
        CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("%PDF"), pdf[..4]);

        string texto = LerTexto(pdf);

        Assert.Contains("Disciplina: Ciências, Prova de recuperação, Série: 8", texto);
        Assert.DoesNotContain("Matéria:", texto);
        Assert.DoesNotContain("Astronomia", texto);
    }

    [TestMethod]
    [TestCategory("HTTP")]
    public async Task EndpointPdf_RecursoInexistente_RedirecionaComMensagemDeErro()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await RegistrarUsuarioAsync(factory, client);
        Guid idInexistente = Guid.CreateVersion7();

        using HttpResponseMessage response = await client.GetAsync($"/Prova/Pdf/{idInexistente}");

        Assert.AreEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.AreEqual("/Prova/Listar", response.Headers.Location?.OriginalString);

        using HttpResponseMessage responseListagem = await client.GetAsync(response.Headers.Location);
        string html = WebUtility.HtmlDecode(await responseListagem.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.OK, responseListagem.StatusCode);
        Assert.Contains("Prova não encontrada.", html);
    }

    private static async Task<Guid> RegistrarUsuarioAsync(
        HttpTestApplicationFactory factory,
        HttpClient client
    )
    {
        string email = $"pdf-{Guid.CreateVersion7():N}@teste.local";
        const string senha = "Senha123!";
        using HttpResponseMessage paginaRegistro = await client.GetAsync("/Autenticacao/Registrar");
        string html = await paginaRegistro.Content.ReadAsStringAsync();
        Match tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
            RegexOptions.CultureInvariant
        );

        Assert.AreEqual(HttpStatusCode.OK, paginaRegistro.StatusCode);
        Assert.IsTrue(tokenMatch.Success, "O formulário de registro não forneceu antiforgery token.");

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Senha"] = senha,
            ["ConfirmarSenha"] = senha,
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(tokenMatch.Groups[1].Value)
        });
        using HttpResponseMessage registro = await client.PostAsync("/Autenticacao/Registrar", form);

        Assert.AreEqual(HttpStatusCode.Found, registro.StatusCode);
        Assert.AreEqual("/", registro.Headers.Location?.OriginalString);

        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<IdentityUser<Guid>> userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser<Guid>>>();
        IdentityUser<Guid>? user = await userManager.FindByEmailAsync(email);

        Assert.IsNotNull(user);
        return user.Id;
    }

    private static async Task<Prova> CadastrarProvaAsync(
        HttpTestApplicationFactory factory,
        Guid userId,
        string titulo,
        bool recuperacao
    )
    {
        var disciplina = new Disciplina("Ciências") { UserId = userId };
        var materia = new Materia("Astronomia", 8, disciplina) { UserId = userId };
        var correta = new Alternativa("Marte", true) { UserId = userId };
        var incorreta = new Alternativa("Vênus", false) { UserId = userId };
        var questao = new Questao(
            "Qual planeta é conhecido como planeta vermelho?",
            materia,
            [correta, incorreta]
        )
        {
            UserId = userId
        };
        var prova = new Prova(
            titulo,
            disciplina,
            recuperacao ? null : materia,
            8,
            1,
            recuperacao,
            [questao]
        )
        {
            UserId = userId
        };

        using IServiceScope scope = factory.Services.CreateScope();
        DbContextOptions<GeradorDeProvasDbContext> options = scope.ServiceProvider
            .GetRequiredService<DbContextOptions<GeradorDeProvasDbContext>>();
        await using var dbContext = new GeradorDeProvasDbContext(
            options,
            new ProvedorDeUsuarioFixo(userId)
        );
        dbContext.Provas.Add(prova);
        await dbContext.SaveChangesAsync();

        return prova;
    }

    private static string LerTexto(byte[] pdf)
    {
        using PdfDocument documento = PdfDocument.Open(pdf);

        return string.Join(
            Environment.NewLine,
            documento.GetPages().Select(pagina => ContentOrderTextExtractor.GetText(pagina, true))
        );
    }

    private static string? ObterNomeArquivo(ContentDispositionHeaderValue? contentDisposition)
    {
        return (contentDisposition?.FileNameStar ?? contentDisposition?.FileName)?.Trim('"');
    }

    private sealed class ProvedorDeUsuarioFixo(Guid id) : IProvedorDeUsuario
    {
        public Guid? Id => id;
        public bool EstaAutenticado => true;
    }
}
