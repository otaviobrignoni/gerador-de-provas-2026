namespace GeradorDeProvas.Aplicacao.ModuloQuestao;

public record ListarQuestaoDto(
    Guid Id,
    string Enunciado,
    string NomeMateria,
    string RespostaCorreta
);

public record CadastrarAlternativaDto(
    string Texto,
    bool Correta
);

public record CadastrarQuestaoDto(
    string Enunciado,
    Guid MateriaId,
    List<CadastrarAlternativaDto> Alternativas
);

public record EditarQuestaoDto(
    Guid Id,
    string Enunciado,
    Guid MateriaId,
    List<CadastrarAlternativaDto> Alternativas
);

public record AlternativaDto(
    Guid Id,
    string Texto,
    bool Correta
);

public record DetalhesQuestaoDto(
    Guid Id,
    string Enunciado,
    Guid MateriaId,
    string NomeMateria,
    List<AlternativaDto> Alternativas
);

public record OpcaoMateriaQuestaoDto(
    Guid Id,
    string Nome
);
