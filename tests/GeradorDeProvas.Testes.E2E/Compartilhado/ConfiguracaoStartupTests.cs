using AutoMapper;
using GeradorDeProvas.Aplicacao;
using GeradorDeProvas.Infra.Compartilhado.Logging;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.WebApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog.Core;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

[TestClass]
public sealed class ConfiguracaoStartupTests
{
    [TestMethod]
    [TestCategory("Infrastructure")]
    public async Task StartupEmDevelopment_SemLicencaNewRelic_IniciaAplicacao()
    {
        string diretorioLogs = CriarCaminhoTemporario();

        try
        {
            await using var factory = new StartupApplicationFactory(
                Environments.Development,
                new Dictionary<string, string?>
                {
                    ["Infra:Serilog:Directory"] = diretorioLogs
                }
            );
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/Autenticacao/Entrar");

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
        finally
        {
            ExcluirDiretorio(diretorioLogs);
        }
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public async Task StartupEmTesting_NaoCriaDiretorioNemArquivoDeLog()
    {
        string diretorioLogs = CriarCaminhoTemporario();
        string marcador = $"testing-sem-arquivo-{Guid.CreateVersion7():N}";

        try
        {
            await using var factory = new StartupApplicationFactory(
                "Testing",
                new Dictionary<string, string?>
                {
                    ["Infra:Serilog:Directory"] = diretorioLogs
                }
            );
            using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using HttpResponseMessage response = await client.GetAsync("/Autenticacao/Entrar");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Infra:NewRelic:Enabled"] = "false",
                    ["Infra:Serilog:Directory"] = diretorioLogs
                })
                .Build();
            using (Logger logger = SerilogFactory.Create(
                configuration,
                new AmbienteDeTeste("Testing")
            ))
                logger.Error("{Marcador}", marcador);

            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.IsFalse(Directory.Exists(diretorioLogs));

            string diretorioPadrao = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GeradorDeProvas"
            );
            string[] arquivosPadrao = Directory.Exists(diretorioPadrao)
                ? Directory.GetFiles(diretorioPadrao, "erro*.log")
                : [];

            foreach (string arquivo in arquivosPadrao)
                Assert.DoesNotContain(marcador, File.ReadAllText(arquivo));
        }
        finally
        {
            ExcluirDiretorio(diretorioLogs);
        }
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public void NewRelicHabilitado_SemLicenca_LancaErroDeConfiguracaoClaro()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infra:NewRelic:Enabled"] = "true",
                ["Infra:NewRelic:LicenseKey"] = null
            })
            .Build();
        var environment = new AmbienteDeTeste("Testing");

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => SerilogFactory.Create(configuration, environment)
        );

        Assert.AreEqual(
            "A chave de licença do NewRelic não foi configurada. Configure Infra:NewRelic:LicenseKey.",
            exception.Message
        );
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public void Serilog_RegistraFileSinkUmaUnicaVez()
    {
        string diretorioLogs = CriarCaminhoTemporario();
        string marcador = $"evento-unico-{Guid.CreateVersion7():N}";
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infra:NewRelic:Enabled"] = "false",
                ["Infra:Serilog:Directory"] = diretorioLogs
            })
            .Build();
        var environment = new AmbienteDeTeste(Environments.Development);

        try
        {
            using (Logger logger = SerilogFactory.Create(configuration, environment))
                logger.Error("{Marcador}", marcador);

            string[] arquivos = Directory.GetFiles(diretorioLogs, "erro*.log");
            Assert.HasCount(1, arquivos);

            string conteudo = File.ReadAllText(arquivos.Single());
            Assert.AreEqual(1, Regex.Matches(conteudo, Regex.Escape(marcador)).Count);
        }
        finally
        {
            ExcluirDiretorio(diretorioLogs);
        }
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public void ConfiguracaoDeAplicacao_NaoRegistraServicosDuplicados()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();

        services.AddApplicationServices(configuration);

        string[] servicosDuplicados = services
            .GroupBy(descriptor => descriptor.ServiceType)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key.FullName ?? grupo.Key.Name)
            .Order()
            .ToArray();

        CollectionAssert.AreEqual(Array.Empty<string>(), servicosDuplicados);
        Assert.HasCount(4, services);
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public void ConfiguracaoDoAutoMapper_EValida()
    {
        using var factory = new StartupApplicationFactory("Testing");
        IMapper mapper = factory.Services.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    public void ConfiguracaoDeAutorizacao_RegistraPoliticaGlobalParaUsuarioAutenticado()
    {
        using var factory = new StartupApplicationFactory("Testing");
        AuthorizationOptions options = factory.Services
            .GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value;

        Assert.IsNotNull(options.FallbackPolicy);
        Assert.HasCount(1, options.FallbackPolicy.Requirements);
        Assert.IsInstanceOfType<DenyAnonymousAuthorizationRequirement>(
            options.FallbackPolicy.Requirements.Single()
        );
    }

    [TestMethod]
    [TestCategory("Infrastructure")]
    [TestCategory("HTTP")]
    public async Task HealthCheck_EstaAcessivelPorEndpointHttpAnonimo()
    {
        await using var factory = new StartupApplicationFactory("Testing");
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using HttpResponseMessage response = await client.GetAsync("/health");
        string conteudo = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", conteudo);
    }

    private static string CriarCaminhoTemporario()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "gerador-de-provas-tests",
            Guid.CreateVersion7().ToString("N")
        );
    }

    private static void ExcluirDiretorio(string caminho)
    {
        if (Directory.Exists(caminho))
            Directory.Delete(caminho, recursive: true);
    }

    private sealed class AmbienteDeTeste(string nome) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nome;
        public string ApplicationName { get; set; } = typeof(Program).Assembly.FullName!;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

internal sealed class StartupApplicationFactory(
    string environmentName,
    IReadOnlyDictionary<string, string?>? configurationOverrides = null
) : WebApplicationFactory<Program>
{
    private readonly string dbName = $"startup-{Guid.CreateVersion7():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environmentName);

        if (configurationOverrides is not null)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationOverrides);
            });
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GeradorDeProvasDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GeradorDeProvasDbContext>>();
            services.AddDbContext<GeradorDeProvasDbContext>(options =>
                options.UseInMemoryDatabase(dbName)
            );
        });
    }
}
