namespace GeradorDeProvas.Aplicacao.ModuloProva;

public record ListarProvaDto(
    Guid Id,
    string Titulo,
    string NomeDisciplina,
    string? NomeMateria,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao
);

public record CadastrarProvaDto(
    string Titulo,
    Guid DisciplinaId,
    Guid? MateriaId,
    int Serie,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao
);

public record DuplicarProvaDto(
    Guid Id,
    string Titulo
);

public record QuestaoProvaDto(
    Guid Id,
    string Enunciado,
    List<AlternativaProvaDto> Alternativas
);

public record AlternativaProvaDto(
    Guid Id,
    string Texto,
    bool Correta
);

public record DetalhesProvaDto(
    Guid Id,
    string Titulo,
    Guid DisciplinaId,
    string NomeDisciplina,
    Guid? MateriaId,
    string? NomeMateria,
    int Serie,
    int QuantidadeQuestoes,
    bool ProvaRecuperacao,
    List<QuestaoProvaDto> Questoes
);

public record OpcaoDisciplinaProvaDto(Guid Id, string Nome);

public record OpcaoMateriaProvaDto(Guid Id, string Nome, int Serie);
