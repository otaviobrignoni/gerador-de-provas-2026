namespace GeradorDeProvas.Aplicacao.ModuloMateria;

public record ListarMateriaDto(
    Guid Id,
    string Nome,
    int Serie,
    string NomeDisciplina
);

public record CadastrarMateriaDto(
    string Nome,
    int Serie,
    Guid DisciplinaId
);

public record EditarMateriaDto(
    Guid Id,
    string Nome,
    int Serie,
    Guid DisciplinaId
);

public record DetalhesMateriaDto(
    Guid Id,
    string Nome,
    int Serie,
    Guid DisciplinaId,
    string NomeDisciplina
);

public record OpcaoDisciplinaMateriaDto(
    Guid Id,
    string Nome
);
