using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace GeradorDeProvas.Infra.Compartilhado.Logging;

public static class SerilogFactory
{
    public static Logger Create(IConfiguration configuration, IHostEnvironment env)
    {
        LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (!env.IsEnvironment("Testing"))
        {
            string caminhoAppData = Environment
                .GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string caminhoDiretorio = configuration["Infra:Serilog:Directory"]
                ?? Path.Combine(caminhoAppData, "GeradorDeProvas");

            Directory.CreateDirectory(caminhoDiretorio);

            string caminhoLogs = Path.Combine(caminhoDiretorio, "erro.log");

            loggerConfiguration.WriteTo.File(
                caminhoLogs,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Error
            );
        }

        NewRelicOptions newRelicOptions = configuration
            .GetSection(NewRelicOptions.SectionName)
            .Get<NewRelicOptions>() ?? new NewRelicOptions();

        if (!newRelicOptions.Enabled)
            return loggerConfiguration.CreateLogger();

        if (string.IsNullOrWhiteSpace(newRelicOptions.LicenseKey))
        {
            throw new InvalidOperationException(
                "A chave de licença do NewRelic não foi configurada. Configure Infra:NewRelic:LicenseKey."
            );
        }

        loggerConfiguration.WriteTo.NewRelicLogs(
            endpointUrl: newRelicOptions.EndpointUrl,
            applicationName: newRelicOptions.ApplicationName,
            licenseKey: newRelicOptions.LicenseKey
        );

        return loggerConfiguration.CreateLogger();
    }
}
