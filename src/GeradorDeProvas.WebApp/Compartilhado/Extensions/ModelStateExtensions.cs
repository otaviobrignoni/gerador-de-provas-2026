using FluentResults;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GeradorDeProvas.WebApp.Compartilhado.Extensions;

public static class ModelStateExtensions
{
    public static void AddModelError(this ModelStateDictionary modelState, ResultBase result)
    {
        foreach (IError erro in result.Errors)
        {
            string campo = erro.Metadata.TryGetValue("Campo", out object? valorCampo)
                && valorCampo is string campoInformado
                    ? campoInformado
                    : string.Empty;

            modelState.AddModelError(campo, erro.Message);
        }
    }
}
