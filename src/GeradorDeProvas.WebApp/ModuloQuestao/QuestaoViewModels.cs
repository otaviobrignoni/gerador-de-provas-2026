using System.ComponentModel.DataAnnotations;

namespace GeradorDeProvas.WebApp.ModuloQuestao;

public record ListarQuestaoViewModel(
    Guid Id,
    string Enunciado,
    string NomeMateria,
    string RespostaCorreta
);

public record AlternativaViewModel(
    [Required(ErrorMessage = "O texto da alternativa deve ser preenchido.")]
    string Texto,
    bool Correta
);

public record CadastrarQuestaoViewModel(
    [Required(ErrorMessage = "O campo \"Enunciado\" deve ser preenchido.")]
    [StringLength(2000, ErrorMessage = "O campo \"Enunciado\" deve conter no máximo 2000 caracteres.")]
    string Enunciado,

    [Required(ErrorMessage = "O campo \"Matéria\" deve ser preenchido.")]
    Guid? MateriaId,

    [Required(ErrorMessage = "Configure as alternativas da questão.")]
    [MinLength(2, ErrorMessage = "Configure no mínimo duas alternativas.")]
    [MaxLength(4, ErrorMessage = "Configure no máximo quatro alternativas.")]
    List<AlternativaViewModel> Alternativas
);

public record EditarQuestaoViewModel(
    Guid Id,

    [Required(ErrorMessage = "O campo \"Enunciado\" deve ser preenchido.")]
    [StringLength(2000, ErrorMessage = "O campo \"Enunciado\" deve conter no máximo 2000 caracteres.")]
    string Enunciado,

    [Required(ErrorMessage = "O campo \"Matéria\" deve ser preenchido.")]
    Guid? MateriaId,

    [Required(ErrorMessage = "Configure as alternativas da questão.")]
    [MinLength(2, ErrorMessage = "Configure no mínimo duas alternativas.")]
    [MaxLength(4, ErrorMessage = "Configure no máximo quatro alternativas.")]
    List<AlternativaViewModel> Alternativas
);

public record ExcluirQuestaoViewModel(
    Guid Id,
    string Enunciado,
    string NomeMateria
);
