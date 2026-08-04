using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Infra.Compartilhado.Logging;
using GeradorDeProvas.Infra.Compartilhado.Orm;
using GeradorDeProvas.Infra.ModuloDisciplina;
using GeradorDeProvas.Infra.ModuloMateria;
using GeradorDeProvas.Infra.ModuloProva;
using GeradorDeProvas.Infra.ModuloQuestao;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace GeradorDeProvas.Infra;

public static class InjecaoDeDependencia
{
    public static void AddInfraRepositories(this IServiceCollection services, IConfiguration configuration, ILoggingBuilder logging, IHostEnvironment env)
    {
        // Injeta logs do Serilog
        Serilog.ILogger logger = SerilogFactory.Create(configuration, env);

        logging.ClearProviders();

        services.AddSerilog(logger, true);

        // Injeta o DbContext do EF
        services.AddDbContext<GeradorDeProvasDbContext>(options =>
        {
            string? connectionString = configuration.GetConnectionString("SqlServerEF");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"A connection string \"SqlServerEF\" não foi encontrada.");

            options.UseSqlServer(connectionString, opt => opt.EnableRetryOnFailure(3));
        });

        // Configuração do Usuário no Identity
        services.AddIdentityCore<IdentityUser<Guid>>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddRoles<IdentityRole<Guid>>() // Configuração de Cargos/Papéis no Identity
        .AddEntityFrameworkStores<GeradorDeProvasDbContext>() // Integração com EntityFramework
        .AddSignInManager() // Configuração do SignInManager
        .AddDefaultTokenProviders();

        services.AddScoped<IRepositorioDisciplina, RepositorioDisciplina>();
        services.AddScoped<IRepositorioMateria, RepositorioMateria>();
        services.AddScoped<IRepositorioQuestao, RepositorioQuestao>();
        services.AddScoped<IRepositorioProva, RepositorioProva>();
    }
}
