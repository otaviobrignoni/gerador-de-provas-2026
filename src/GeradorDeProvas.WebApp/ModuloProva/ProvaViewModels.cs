using System.ComponentModel.DataAnnotations;
using GeradorDeProvas.Dominio.ModuloProva;

namespace GeradorDeProvas.WebApp.ModuloProva;

public record ListarProvaViewModel(
    Guid Id,
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao
);

public record CadastrarProvaEtapa1ViewModel(
    [Required(ErrorMessage = "O campo \"Título\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Título\" deve conter entre 2 e 100 caracteres.")]
    string Titulo,
    [Required(ErrorMessage = "O campo \"Disciplina\" deve ser preenchido.")]
    Guid? DisciplinaId,
    [Required(ErrorMessage = "O campo \"Série\" deve ser preenchido.")]
    [Range(1, int.MaxValue, ErrorMessage = "O campo \"Série\" deve ser maior que zero.")]
    int? Serie,
    bool ProvaRecuperacao
);

public record CadastrarProvaEtapa2ViewModel(
    string Titulo,
    string NomeDisciplina,
    int Serie,
    bool ProvaRecuperacao,
    Guid? MateriaId,
    [Required(ErrorMessage = "Informe a quantidade de questões.")]
    [Range(1, Prova.QuantidadeMaximaQuestoes, ErrorMessage = "A quantidade de questões deve estar entre 1 e 60.")]
    int? QuantidadeQuestoes
);

public record DuplicarProvaViewModel(
    Guid Id,
    [Required(ErrorMessage = "O campo \"Título\" deve ser preenchido.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O campo \"Título\" deve conter entre 2 e 100 caracteres.")]
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int Serie,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao
);

public record AlternativaProvaViewModel(Guid Id, string Texto, bool Correta);

public record QuestaoProvaViewModel(
    Guid Id,
    string Enunciado,
    List<AlternativaProvaViewModel> Alternativas
);

public record ConfirmarProvaViewModel(
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int Serie,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao,
    List<QuestaoProvaViewModel> Questoes
);

public record DetalhesProvaViewModel(
    Guid Id,
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int Serie,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao,
    List<QuestaoProvaViewModel> Questoes
);

public record ExcluirProvaViewModel(
    Guid Id,
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao
);
