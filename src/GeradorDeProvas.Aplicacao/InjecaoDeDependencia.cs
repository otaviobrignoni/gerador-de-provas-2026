using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using GeradorDeProvas.Aplicacao.ModuloDisciplina;
using GeradorDeProvas.Aplicacao.ModuloMateria;
using GeradorDeProvas.Aplicacao.ModuloQuestao;

namespace GeradorDeProvas.Aplicacao;

public static class InjecaoDeDependencia
{
    public static void AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<ServicoDisciplina>();
        services.AddScoped<ServicoMateria>();
        services.AddScoped<ServicoQuestao>();
    }
}
