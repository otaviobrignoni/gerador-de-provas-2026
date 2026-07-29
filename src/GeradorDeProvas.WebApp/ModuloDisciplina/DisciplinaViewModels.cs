using System.ComponentModel.DataAnnotations;

namespace GeradorDeProvas.WebApp.ModuloDisciplina;

public record ListarDisciplinaViewModel(
    Guid Id,
    string Nome
);

public record CadastrarDisciplinaViewModel(
    [Required(ErrorMessage = "O campo \"Nome\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Nome\" deve conter entre 2 e 100 caracteres.")]
    string Nome
);

public record EditarDisciplinaViewModel(
    Guid Id,

    [Required(ErrorMessage = "O campo \"Nome\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Nome\" deve conter entre 2 e 100 caracteres.")]
    string Nome
);

public record ExcluirDisciplinaViewModel(
    Guid Id,
    string Nome
);
