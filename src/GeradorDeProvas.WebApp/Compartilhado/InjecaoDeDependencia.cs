using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.WebApp.Compartilhado.Identity;
using GeradorDeProvas.WebApp.Compartilhado.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GeradorDeProvas.WebApp.Compartilhado;

public static class InjecaoDeDependencia
{
    public static void AddPresentationConfig(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddControllersWithViews().AddRazorOptions(options =>
        {
            // Reseta a configuração padrão do MVC
            options.ViewLocationFormats.Clear();

            // Localização das Views dos módulos: ModuloCaixa/Views/Listar.cshtml
            options.ViewLocationFormats.Add("/Modulo{1}/Views/{0}.cshtml");

            // Localização das Views compartilhadas: /Compartilhado/Views/_Layout.cshtml
            options.ViewLocationFormats.Add("/Compartilhado/Views/{0}.cshtml");
        });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
            options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
        }).AddCookie(IdentityConstants.ApplicationScheme, cookieOptions =>
        {
            cookieOptions.LoginPath = "/Autenticacao/Entrar";
            cookieOptions.AccessDeniedPath = "/Autenticacao/Entrar";
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IProvedorDeUsuario, ProvedorDeUsuario>();

        services.AddAutoMapper(mapperConfig =>
        {
            AutoMapperOptions autoMapperOptions = configuration
                .GetSection(AutoMapperOptions.SectionName)
                .Get<AutoMapperOptions>() ?? new AutoMapperOptions();

            string? licenseKey = autoMapperOptions.LicenseKey;

            if (!string.IsNullOrWhiteSpace(licenseKey))
                mapperConfig.LicenseKey = licenseKey;

            mapperConfig.AddMaps(typeof(Program));
        });
    }
}
