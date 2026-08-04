namespace GeradorDeProvas.Infra.Compartilhado.Logging;

public sealed class NewRelicOptions
{
    public const string SectionName = "Infra:NewRelic";

    public bool Enabled { get; set; }
    public string EndpointUrl { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string? LicenseKey { get; set; }
}
