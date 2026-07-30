using GeradorDeProvas.Aplicacao.ModuloDisciplina;
using GeradorDeProvas.Aplicacao.ModuloMateria;
using GeradorDeProvas.Aplicacao.ModuloProva;
using GeradorDeProvas.Aplicacao.ModuloQuestao;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeradorDeProvas.Aplicacao;

public static class InjecaoDeDependencia
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ServicoDisciplina>();
        services.AddScoped<ServicoMateria>();
        services.AddScoped<ServicoQuestao>();
        services.AddScoped<ServicoQuestao>();
        services.AddScoped<ServicoProva>();
    }
}
