using GeradorDeProvas.Aplicacao;
using GeradorDeProvas.Infra;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.WebApp.Compartilhado;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GeradorDeProvas.WebApp;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuração do container de injeção de dependência
        builder.Services.AddInfraRepositories(builder.Configuration, builder.Logging);
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddPresentationConfig(builder.Configuration);

        // Configura health checks do banco de dados
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<GeradorDeProvasDbContext>(
                name: "database_check",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]
            );

        var app = builder.Build();

        // Aplica migrações automaticamente em Desenvolvimento
        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<GeradorDeProvasDbContext>();

            dbContext.Database.Migrate();
        }

        // Middlewares de roteamento
        app.UseRouting();

        // Middlewares de Auth
        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware de reconhecimento de rotas de controllers
        app.MapDefaultControllerRoute();

        // Execução do Servidor
        app.Run();
    }
}
