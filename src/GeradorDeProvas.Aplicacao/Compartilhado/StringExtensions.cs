namespace GeradorDeProvas.Aplicacao.Compartilhado;

public static class StringExtensions
{
    public static string Normalizar(this string valor)
    {
        return valor.Trim().ToLowerInvariant();
    }
}
