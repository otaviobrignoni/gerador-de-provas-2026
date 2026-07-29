using System.ComponentModel.DataAnnotations;

namespace GeradorDeProvas.WebApp.ModuloAutenticacao;

public record RegistrarViewModel
{
    [Required(ErrorMessage = "O campo \"E-mail\" deve ser preenchido.")]
    [EmailAddress(ErrorMessage = "O campo \"E-mail\" deve conter um endereço de e-mail válido.")]
    [StringLength(256, ErrorMessage = "O campo \"E-mail\" deve conter no máximo 256 caracteres.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "O campo \"Senha\" deve ser preenchido.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "O campo \"Senha\" deve conter no mínimo 8 caracteres.")]
    public string Senha { get; init; } = string.Empty;

    [Required(ErrorMessage = "O campo \"Confirmar Senha\" deve ser preenchido.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
    public string ConfirmarSenha { get; init; } = string.Empty;
}

public record EntrarViewModel
{
    [Required(ErrorMessage = "O campo \"E-mail\" deve ser preenchido.")]
    [EmailAddress(ErrorMessage = "O campo \"E-mail\" deve conter um endereço de e-mail válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "O campo \"Senha\" deve ser preenchido.")]
    [DataType(DataType.Password)]
    public string Senha { get; init; } = string.Empty;

    public bool LembrarMe { get; init; }

    public string? ReturnUrl { get; init; }
}

