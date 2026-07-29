using System.ComponentModel.DataAnnotations;

namespace GeradorDeProvas.WebApp.ModuloMateria;

public record ListarMateriaViewModel(
    Guid Id,
    string Nome,
    int Serie,
    string NomeDisciplina
);

public record CadastrarMateriaViewModel(
    [Required(ErrorMessage = "O campo \"Nome\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Nome\" deve conter entre 2 e 100 caracteres.")]
    string Nome,

    [Required(ErrorMessage = "O campo \"Disciplina\" deve ser preenchido.")]
    Guid? DisciplinaId,

    [Required(ErrorMessage = "O campo \"Série\" deve ser preenchido.")]
    [Range(1, int.MaxValue, ErrorMessage = "O campo \"Série\" deve ser maior que zero.")]
    int? Serie
);

public record EditarMateriaViewModel(
    Guid Id,

    [Required(ErrorMessage = "O campo \"Nome\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Nome\" deve conter entre 2 e 100 caracteres.")]
    string Nome,

    [Required(ErrorMessage = "O campo \"Disciplina\" deve ser preenchido.")]
    Guid? DisciplinaId,

    [Required(ErrorMessage = "O campo \"Série\" deve ser preenchido.")]
    [Range(1, int.MaxValue, ErrorMessage = "O campo \"Série\" deve ser maior que zero.")]
    int? Serie
);

public record ExcluirMateriaViewModel(
    Guid Id,
    string Nome,
    int Serie,
    string NomeDisciplina
);
