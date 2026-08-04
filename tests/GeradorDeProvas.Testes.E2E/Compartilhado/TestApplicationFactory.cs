using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.WebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

public sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string dbName;
    public string Url { get; }

    public TestApplicationFactory()
    {
        dbName = $"e2e-{Guid.NewGuid():N}";

        UseKestrel(0);
        StartServer();

        Url = GetKestrelUrl();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GeradorDeProvasDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GeradorDeProvasDbContext>>();
            services.AddDbContext<GeradorDeProvasDbContext>(options => options.UseInMemoryDatabase(dbName));
        });
    }

    private string GetKestrelUrl()
    {
        var server = Services.GetRequiredService<IServer>();

        var ex = new InvalidOperationException("Não foi possível obter a URL do servidor");

        return server.Features.Get<IServerAddressesFeature>()?.Addresses.Single() ?? throw ex;
    }
}
