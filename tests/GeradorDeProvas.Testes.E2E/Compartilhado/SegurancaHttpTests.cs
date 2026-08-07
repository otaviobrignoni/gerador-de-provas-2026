using System.Net;
using System.Text.Json;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
[TestCategory("HTTP")]
[TestCategory("Security")]
public sealed class SegurancaHttpTests
{
    private const string SenhaValida = "Senha123!";

    [TestMethod]
    [DataRow("/Disciplina/Cadastrar")]
    [DataRow("/Disciplina/Excluir")]
    [DataRow("/Materia/Excluir")]
    [DataRow("/Questao/Excluir")]
    [DataRow("/Prova/Excluir")]
    [DataRow("/Prova/Duplicar")]
    [DataRow("/Prova/Confirmar")]
    [DataRow("/Autenticacao/Sair")]
    public async Task PostSemAntiforgeryToken_AcoesSensiveis_RetornamBadRequest(string caminho)
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("antiforgery"));

        using HttpResponseMessage response = await client.PostAsync(
            caminho,
            new FormUrlEncodedContent([])
        );

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task PostComAntiforgeryTokenValido_CadastroProtegidoEAceito()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        Guid usuarioId = await CriarUsuarioEEntrarAsync(
            factory,
            client,
            CriarEmail("token-valido")
        );
        string nome = $"Disciplina {Guid.CreateVersion7():N}";
        string token = await ObterAntiforgeryTokenAsync(client, "/Disciplina/Cadastrar");

        using HttpResponseMessage response = await client.PostAsync(
            "/Disciplina/Cadastrar",
            CriarFormulario(token, ("Nome", nome))
        );

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/Disciplina/Listar", response.Headers.Location?.OriginalString);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        GeradorDeProvasDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<GeradorDeProvasDbContext>();
        Disciplina disciplina = await dbContext.Disciplinas
            .IgnoreQueryFilters()
            .SingleAsync(d => d.Nome == nome);
        Assert.AreEqual(usuarioId, disciplina.UserId);
    }

    [TestMethod]
    [DataRow("/Disciplina/Listar")]
    [DataRow("/Materia/Listar")]
    [DataRow("/Questao/Listar")]
    [DataRow("/Prova/Listar")]
    public async Task UsuarioAnonimo_RotaProtegida_RedirecionaParaLoginComReturnUrlExata(
        string caminho
    )
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();

        using HttpResponseMessage response = await client.GetAsync(caminho);

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual(
            $"https://localhost/Autenticacao/Entrar?ReturnUrl={Uri.EscapeDataString(caminho)}",
            response.Headers.Location?.OriginalString
        );
    }

    [TestMethod]
    public async Task LogoutComTokenValido_EncerraSessaoERestauraProtecaoDasRotas()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("logout"));
        string token = await ObterAntiforgeryTokenAsync(client, "/");

        using HttpResponseMessage responseLogout = await client.PostAsync(
            "/Autenticacao/Sair",
            CriarFormulario(token)
        );

        Assert.AreEqual(HttpStatusCode.Redirect, responseLogout.StatusCode);
        Assert.AreEqual(
            "/Autenticacao/Entrar",
            responseLogout.Headers.Location?.OriginalString
        );

        const string caminhoProtegido = "/Questao/Listar";
        using HttpResponseMessage responseProtegido = await client.GetAsync(caminhoProtegido);
        Assert.AreEqual(HttpStatusCode.Redirect, responseProtegido.StatusCode);
        Assert.AreEqual(
            $"https://localhost/Autenticacao/Entrar?ReturnUrl={Uri.EscapeDataString(caminhoProtegido)}",
            responseProtegido.Headers.Location?.OriginalString
        );
    }

    [TestMethod]
    public async Task LoginComReturnUrlExterna_RedirecionaSomenteParaHomeLocal()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        string email = CriarEmail("return-url-externa");
        await CriarUsuarioAsync(factory, email, Guid.CreateVersion7());
        const string returnUrlExterna = "https://example.com/nao-confiavel";
        string token = await ObterAntiforgeryTokenAsync(
            client,
            $"/Autenticacao/Entrar?returnUrl={Uri.EscapeDataString(returnUrlExterna)}"
        );

        using HttpResponseMessage response = await client.PostAsync(
            "/Autenticacao/Entrar",
            CriarFormulario(
                token,
                ("Email", email),
                ("Senha", SenhaValida),
                ("LembrarMe", "false"),
                ("ReturnUrl", returnUrlExterna)
            )
        );

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/", response.Headers.Location?.OriginalString);

        using HttpResponseMessage home = await client.GetAsync("/");
        Assert.AreEqual(HttpStatusCode.OK, home.StatusCode);
    }

    [TestMethod]
    public async Task UsuarioNaoPodeConsultarRecursosDeOutroUsuarioUsandoIds()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("intruso-consulta"));
        GrafoDeProva grafo = SalvarGrafo(factory, Guid.CreateVersion7(), "consulta");

        var caminhos = new Dictionary<string, string>
        {
            [$"/Disciplina/Editar/{grafo.DisciplinaId}"] = "/Disciplina/Listar",
            [$"/Disciplina/Excluir/{grafo.DisciplinaId}"] = "/Disciplina/Listar",
            [$"/Materia/Editar/{grafo.MateriaId}"] = "/Materia/Listar",
            [$"/Materia/Excluir/{grafo.MateriaId}"] = "/Materia/Listar",
            [$"/Questao/Editar/{grafo.QuestaoId}"] = "/Questao/Listar",
            [$"/Questao/Excluir/{grafo.QuestaoId}"] = "/Questao/Listar",
            [$"/Prova/Detalhes/{grafo.ProvaId}"] = "/Prova/Listar",
            [$"/Prova/Pdf/{grafo.ProvaId}"] = "/Prova/Listar",
            [$"/Prova/Gabarito/{grafo.ProvaId}"] = "/Prova/Listar",
            [$"/Prova/Duplicar/{grafo.ProvaId}"] = "/Prova/Listar",
            [$"/Prova/Excluir/{grafo.ProvaId}"] = "/Prova/Listar"
        };

        foreach ((string caminho, string destinoEsperado) in caminhos)
        {
            using HttpResponseMessage response = await client.GetAsync(caminho);

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, caminho);
            Assert.AreEqual(
                destinoEsperado,
                response.Headers.Location?.OriginalString,
                caminho
            );
        }
    }

    [TestMethod]
    public async Task UsuarioNaoPodeEditarDisciplinaDeOutroUsuarioUsandoId()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("intruso-edicao"));
        GrafoDeProva grafo = SalvarGrafo(factory, Guid.CreateVersion7(), "edicao");
        string token = await ObterAntiforgeryTokenAsync(client, "/Disciplina/Cadastrar");
        string nomeAdulterado = $"Adulterada {Guid.CreateVersion7():N}";

        using HttpResponseMessage response = await client.PostAsync(
            "/Disciplina/Editar",
            CriarFormulario(
                token,
                ("Id", grafo.DisciplinaId.ToString()),
                ("Nome", nomeAdulterado)
            )
        );

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(grafo.NomeDisciplina, ObterNomeDisciplina(factory, grafo.DisciplinaId));
    }

    [TestMethod]
    public async Task UsuarioNaoPodeEditarMateriaOuQuestaoDeOutroUsuarioUsandoIds()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        Guid intrusoId = await CriarUsuarioEEntrarAsync(
            factory,
            client,
            CriarEmail("intruso-edicao-relacionada")
        );
        DependenciasEdicao dependencias = SalvarDependenciasEdicao(factory, intrusoId);
        GrafoDeProva grafo = SalvarGrafo(factory, Guid.CreateVersion7(), "edicao-relacionada");
        string token = await ObterAntiforgeryTokenAsync(client, "/Disciplina/Cadastrar");

        using HttpResponseMessage responseMateria = await client.PostAsync(
            "/Materia/Editar",
            CriarFormulario(
                token,
                ("Id", grafo.MateriaId.ToString()),
                ("Nome", $"Matéria adulterada {Guid.CreateVersion7():N}"),
                ("DisciplinaId", dependencias.DisciplinaId.ToString()),
                ("Serie", "8")
            )
        );
        Assert.AreEqual(HttpStatusCode.OK, responseMateria.StatusCode);

        using HttpResponseMessage responseQuestao = await client.PostAsync(
            "/Questao/Editar",
            CriarFormulario(
                token,
                ("Id", grafo.QuestaoId.ToString()),
                ("Enunciado", $"Questão adulterada {Guid.CreateVersion7():N}"),
                ("MateriaId", dependencias.MateriaId.ToString()),
                ("Alternativas[0].Texto", $"Nova correta {Guid.CreateVersion7():N}"),
                ("Alternativas[0].Correta", "true"),
                ("Alternativas[1].Texto", $"Nova incorreta {Guid.CreateVersion7():N}"),
                ("Alternativas[1].Correta", "false")
            )
        );
        string htmlQuestao = WebUtility.HtmlDecode(
            await responseQuestao.Content.ReadAsStringAsync()
        );
        Assert.AreEqual(HttpStatusCode.OK, responseQuestao.StatusCode);
        StringAssert.Contains(htmlQuestao, "Questão não encontrada.");

        using IServiceScope scope = factory.Services.CreateScope();
        GeradorDeProvasDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<GeradorDeProvasDbContext>();
        Materia materia = dbContext.Materias
            .IgnoreQueryFilters()
            .Include(m => m.Disciplina)
            .Single(m => m.Id == grafo.MateriaId);
        Questao questao = dbContext.Questoes
            .IgnoreQueryFilters()
            .Include(q => q.Materia)
            .Include(q => q.Alternativas)
            .Single(q => q.Id == grafo.QuestaoId);

        Assert.AreEqual(grafo.NomeMateria, materia.Nome);
        Assert.AreEqual(7, materia.Serie);
        Assert.AreEqual(grafo.DisciplinaId, materia.Disciplina.Id);
        Assert.AreEqual(grafo.EnunciadoQuestao, questao.Enunciado);
        Assert.AreEqual(grafo.MateriaId, questao.Materia.Id);
        CollectionAssert.AreEquivalent(
            grafo.TextosAlternativas,
            questao.Alternativas.Select(a => a.Texto).ToList()
        );
    }

    [TestMethod]
    public async Task UsuarioNaoPodeDuplicarProvaDeOutroUsuarioUsandoId()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("intruso-duplicacao"));
        GrafoDeProva grafo = SalvarGrafo(factory, Guid.CreateVersion7(), "duplicacao");
        string token = await ObterAntiforgeryTokenAsync(client, "/Disciplina/Cadastrar");
        string tituloAdulterado = $"Cópia alheia {Guid.CreateVersion7():N}";

        using HttpResponseMessage response = await client.PostAsync(
            "/Prova/Duplicar",
            CriarFormulario(
                token,
                ("Id", grafo.ProvaId.ToString()),
                ("Titulo", tituloAdulterado),
                ("NomeDisciplina", grafo.NomeDisciplina),
                ("NomeMateria", grafo.NomeMateria),
                ("Serie", "7"),
                ("QuantidadeQuestoes", "1"),
                ("ProvaRecuperacao", "false")
            )
        );
        string html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(html, "Prova não encontrada.");

        using IServiceScope scope = factory.Services.CreateScope();
        GeradorDeProvasDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<GeradorDeProvasDbContext>();
        List<Prova> provas = dbContext.Provas.IgnoreQueryFilters().ToList();
        Assert.HasCount(1, provas);
        Assert.AreEqual(grafo.ProvaId, provas.Single().Id);
        Assert.AreEqual(grafo.TituloProva, provas.Single().Titulo);
    }

    [TestMethod]
    public async Task UsuarioNaoPodeExcluirRecursosDeOutroUsuarioUsandoIds()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        await CriarUsuarioEEntrarAsync(factory, client, CriarEmail("intruso-exclusao"));
        GrafoDeProva grafo = SalvarGrafo(factory, Guid.CreateVersion7(), "exclusao");
        string token = await ObterAntiforgeryTokenAsync(client, "/Disciplina/Cadastrar");

        (string Caminho, Guid Id, string Destino, string Mensagem)[] requisicoes =
        [
            ("/Disciplina/Excluir", grafo.DisciplinaId, "/Disciplina/Listar", "Disciplina não encontrada."),
            ("/Materia/Excluir", grafo.MateriaId, "/Materia/Listar", "Matéria não encontrada."),
            ("/Questao/Excluir", grafo.QuestaoId, "/Questao/Listar", "Questão não encontrada."),
            ("/Prova/Excluir", grafo.ProvaId, "/Prova/Listar", "Prova não encontrada.")
        ];

        foreach ((string caminho, Guid id, string destino, string mensagem) in requisicoes)
        {
            using HttpResponseMessage response = await client.PostAsync(
                caminho,
                CriarFormulario(token, ("Id", id.ToString()))
            );

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode, caminho);
            Assert.AreEqual(destino, response.Headers.Location?.OriginalString, caminho);

            using HttpResponseMessage paginaDestino = await client.GetAsync(destino);
            string html = WebUtility.HtmlDecode(
                await paginaDestino.Content.ReadAsStringAsync()
            );
            Assert.AreEqual(HttpStatusCode.OK, paginaDestino.StatusCode, caminho);
            StringAssert.Contains(html, mensagem, caminho);
        }

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        GeradorDeProvasDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<GeradorDeProvasDbContext>();
        Assert.IsTrue(await dbContext.Disciplinas.IgnoreQueryFilters().AnyAsync(d => d.Id == grafo.DisciplinaId));
        Assert.IsTrue(await dbContext.Materias.IgnoreQueryFilters().AnyAsync(m => m.Id == grafo.MateriaId));
        Assert.IsTrue(await dbContext.Questoes.IgnoreQueryFilters().AnyAsync(q => q.Id == grafo.QuestaoId));
        Assert.IsTrue(await dbContext.Provas.IgnoreQueryFilters().AnyAsync(p => p.Id == grafo.ProvaId));
    }

    [TestMethod]
    public async Task SelecionarMaterias_RespeitaUsuarioDisciplinaESerie()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient();
        Guid usuarioId = Guid.CreateVersion7();
        await CriarUsuarioEEntrarAsync(
            factory,
            client,
            CriarEmail("selecao-materias"),
            usuarioId
        );
        DadosDeMaterias dados = SalvarMateriasParaSelecao(factory, usuarioId);

        using HttpResponseMessage response = await client.GetAsync(
            $"/Prova/SelecionarMaterias?disciplinaId={dados.DisciplinaId}&serie=7"
        );
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        List<JsonElement> materias = json.RootElement.EnumerateArray().ToList();
        Assert.HasCount(1, materias);
        JsonElement materia = materias.Single();
        Assert.AreEqual(dados.MateriaEsperadaId, materia.GetProperty("id").GetGuid());
        Assert.AreEqual(dados.NomeMateriaEsperada, materia.GetProperty("nome").GetString());

        using HttpResponseMessage responseOutroUsuario = await client.GetAsync(
            $"/Prova/SelecionarMaterias?disciplinaId={dados.DisciplinaOutroUsuarioId}&serie=7"
        );
        using JsonDocument jsonOutroUsuario = JsonDocument.Parse(
            await responseOutroUsuario.Content.ReadAsStringAsync()
        );

        Assert.AreEqual(HttpStatusCode.OK, responseOutroUsuario.StatusCode);
        Assert.HasCount(0, jsonOutroUsuario.RootElement.EnumerateArray().ToList());
    }

    [TestMethod]
    public async Task LoginHttps_EmiteCookieDeAutenticacaoComAtributosSeguros()
    {
        await using var factory = new HttpTestApplicationFactory();
        using HttpClient client = factory.CreateHttpsClient(handleCookies: true);
        string email = CriarEmail("cookie-seguro");
        await CriarUsuarioAsync(factory, email, Guid.CreateVersion7());
        string token = await ObterAntiforgeryTokenAsync(client, "/Autenticacao/Entrar");

        using HttpResponseMessage response = await client.PostAsync(
            "/Autenticacao/Entrar",
            CriarFormulario(
                token,
                ("Email", email),
                ("Senha", SenhaValida),
                ("LembrarMe", "false"),
                ("ReturnUrl", string.Empty)
            )
        );

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        List<string> cookiesDeAutenticacao = response.Headers.GetValues("Set-Cookie")
            .Where(valor => valor.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal))
            .ToList();
        Assert.HasCount(1, cookiesDeAutenticacao);
        string cookie = cookiesDeAutenticacao.Single();
        string cookieNormalizado = cookie.ToLowerInvariant();
        StringAssert.Contains(cookieNormalizado, "; path=/");
        StringAssert.Contains(cookieNormalizado, "; secure");
        StringAssert.Contains(cookieNormalizado, "; samesite=lax");
        StringAssert.Contains(cookieNormalizado, "; httponly");

        using IServiceScope scope = factory.Services.CreateScope();
        CookieAuthenticationOptions options = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.IsTrue(options.Cookie.HttpOnly);
        Assert.AreEqual(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.AreEqual(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);
    }

    private static async Task<Guid> CriarUsuarioEEntrarAsync(
        HttpTestApplicationFactory factory,
        HttpClient client,
        string email,
        Guid? usuarioId = null
    )
    {
        Guid id = usuarioId ?? Guid.CreateVersion7();
        await CriarUsuarioAsync(factory, email, id);
        string token = await ObterAntiforgeryTokenAsync(client, "/Autenticacao/Entrar");

        using HttpResponseMessage response = await client.PostAsync(
            "/Autenticacao/Entrar",
            CriarFormulario(
                token,
                ("Email", email),
                ("Senha", SenhaValida),
                ("LembrarMe", "false"),
                ("ReturnUrl", string.Empty)
            )
        );

        Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.AreEqual("/", response.Headers.Location?.OriginalString);
        return id;
    }

    private static async Task CriarUsuarioAsync(
        HttpTestApplicationFactory factory,
        string email,
        Guid usuarioId
    )
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<IdentityUser<Guid>> userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var usuario = new IdentityUser<Guid>
        {
            Id = usuarioId,
            UserName = email,
            Email = email
        };

        IdentityResult resultado = await userManager.CreateAsync(usuario, SenhaValida);

        Assert.IsTrue(
            resultado.Succeeded,
            string.Join("; ", resultado.Errors.Select(erro => erro.Description))
        );
    }

    private static async Task<string> ObterAntiforgeryTokenAsync(
        HttpClient client,
        string caminho
    )
    {
        using HttpResponseMessage response = await client.GetAsync(caminho);
        string html = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, html);
        Match match = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\""
        );
        Assert.IsTrue(match.Success, $"Token antiforgery não encontrado em {caminho}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static FormUrlEncodedContent CriarFormulario(
        string token,
        params (string Chave, string Valor)[] campos
    )
    {
        return new FormUrlEncodedContent(
            campos
                .Append((Chave: "__RequestVerificationToken", Valor: token))
                .Select(campo => new KeyValuePair<string, string>(campo.Chave, campo.Valor))
        );
    }

    private static GrafoDeProva SalvarGrafo(
        HttpTestApplicationFactory factory,
        Guid usuarioId,
        string sufixo
    )
    {
        using IServiceScope scope = factory.Services.CreateScope();
        DbContextOptions<GeradorDeProvasDbContext> options = scope.ServiceProvider
            .GetRequiredService<DbContextOptions<GeradorDeProvasDbContext>>();
        using var dbContext = new GeradorDeProvasDbContext(options, new ProvedorFixo(usuarioId));
        string nomeDisciplina = $"Disciplina {sufixo} {Guid.CreateVersion7():N}";
        string nomeMateria = $"Matéria {sufixo} {Guid.CreateVersion7():N}";
        string enunciadoQuestao = $"Questão {sufixo} {Guid.CreateVersion7():N}";
        string textoCorreta = $"Correta {sufixo} {Guid.CreateVersion7():N}";
        string textoIncorreta = $"Incorreta {sufixo} {Guid.CreateVersion7():N}";
        string tituloProva = $"Prova {sufixo} {Guid.CreateVersion7():N}";
        var disciplina = new Disciplina(nomeDisciplina);
        var materia = new Materia(nomeMateria, 7, disciplina);
        var questao = new Questao(
            enunciadoQuestao,
            materia,
            [new Alternativa(textoCorreta, true), new Alternativa(textoIncorreta, false)]
        );
        var prova = new Prova(
            tituloProva,
            disciplina,
            materia,
            7,
            1,
            false,
            [questao]
        );

        dbContext.Provas.Add(prova);
        dbContext.SaveChanges();

        return new GrafoDeProva(
            disciplina.Id,
            disciplina.Nome,
            materia.Id,
            materia.Nome,
            questao.Id,
            questao.Enunciado,
            questao.Alternativas.Select(a => a.Texto).ToList(),
            prova.Id,
            prova.Titulo
        );
    }

    private static DependenciasEdicao SalvarDependenciasEdicao(
        HttpTestApplicationFactory factory,
        Guid usuarioId
    )
    {
        using IServiceScope scope = factory.Services.CreateScope();
        DbContextOptions<GeradorDeProvasDbContext> options = scope.ServiceProvider
            .GetRequiredService<DbContextOptions<GeradorDeProvasDbContext>>();
        using var dbContext = new GeradorDeProvasDbContext(options, new ProvedorFixo(usuarioId));
        var disciplina = new Disciplina($"Disciplina própria {Guid.CreateVersion7():N}");
        var materia = new Materia($"Matéria própria {Guid.CreateVersion7():N}", 7, disciplina);
        dbContext.Materias.Add(materia);
        dbContext.SaveChanges();

        return new DependenciasEdicao(disciplina.Id, materia.Id);
    }

    private static DadosDeMaterias SalvarMateriasParaSelecao(
        HttpTestApplicationFactory factory,
        Guid usuarioId
    )
    {
        Guid outroUsuarioId = Guid.CreateVersion7();
        string nomeEsperado = $"Matéria esperada {Guid.CreateVersion7():N}";

        using IServiceScope scope = factory.Services.CreateScope();
        DbContextOptions<GeradorDeProvasDbContext> options = scope.ServiceProvider
            .GetRequiredService<DbContextOptions<GeradorDeProvasDbContext>>();

        Guid disciplinaId;
        Guid materiaEsperadaId;
        using (var dbContext = new GeradorDeProvasDbContext(options, new ProvedorFixo(usuarioId)))
        {
            var disciplina = new Disciplina($"Disciplina alvo {Guid.CreateVersion7():N}");
            var materiaEsperada = new Materia(nomeEsperado, 7, disciplina);
            var materiaOutraSerie = new Materia(
                $"Matéria outra série {Guid.CreateVersion7():N}",
                8,
                disciplina
            );
            var outraDisciplina = new Disciplina($"Outra disciplina {Guid.CreateVersion7():N}");
            var materiaOutraDisciplina = new Materia(
                $"Matéria outra disciplina {Guid.CreateVersion7():N}",
                7,
                outraDisciplina
            );
            dbContext.AddRange(materiaEsperada, materiaOutraSerie, materiaOutraDisciplina);
            dbContext.SaveChanges();
            disciplinaId = disciplina.Id;
            materiaEsperadaId = materiaEsperada.Id;
        }

        Guid disciplinaOutroUsuarioId;
        using (var dbContext = new GeradorDeProvasDbContext(options, new ProvedorFixo(outroUsuarioId)))
        {
            var disciplinaOutroUsuario = new Disciplina(
                $"Disciplina alheia {Guid.CreateVersion7():N}"
            );
            var materiaOutroUsuario = new Materia(
                $"Matéria alheia {Guid.CreateVersion7():N}",
                7,
                disciplinaOutroUsuario
            );
            dbContext.Materias.Add(materiaOutroUsuario);
            dbContext.SaveChanges();
            disciplinaOutroUsuarioId = disciplinaOutroUsuario.Id;
        }

        return new DadosDeMaterias(
            disciplinaId,
            materiaEsperadaId,
            nomeEsperado,
            disciplinaOutroUsuarioId
        );
    }

    private static string ObterNomeDisciplina(
        HttpTestApplicationFactory factory,
        Guid disciplinaId
    )
    {
        using IServiceScope scope = factory.Services.CreateScope();
        GeradorDeProvasDbContext dbContext = scope.ServiceProvider
            .GetRequiredService<GeradorDeProvasDbContext>();
        return dbContext.Disciplinas
            .IgnoreQueryFilters()
            .Single(d => d.Id == disciplinaId)
            .Nome;
    }

    private static string CriarEmail(string prefixo) =>
        $"{prefixo}.{Guid.CreateVersion7():N}@teste.local";

    private sealed class ProvedorFixo(Guid id) : IProvedorDeUsuario
    {
        public Guid? Id => id;
        public bool EstaAutenticado => true;
    }

    private sealed record GrafoDeProva(
        Guid DisciplinaId,
        string NomeDisciplina,
        Guid MateriaId,
        string NomeMateria,
        Guid QuestaoId,
        string EnunciadoQuestao,
        List<string> TextosAlternativas,
        Guid ProvaId,
        string TituloProva
    );

    private sealed record DependenciasEdicao(Guid DisciplinaId, Guid MateriaId);

    private sealed record DadosDeMaterias(
        Guid DisciplinaId,
        Guid MateriaEsperadaId,
        string NomeMateriaEsperada,
        Guid DisciplinaOutroUsuarioId
    );
}
