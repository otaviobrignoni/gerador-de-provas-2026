using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.WebApp;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GeradorDeProvas.Testes.E2E.Compartilhado;

public sealed class HttpTestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"http-{Guid.CreateVersion7():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<GeradorDeProvasDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<GeradorDeProvasDbContext>>();
            services.AddDbContext<GeradorDeProvasDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
            );
        });
    }

    public HttpClient CreateHttpsClient(bool handleCookies = true)
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = handleCookies
        });
    }
}
