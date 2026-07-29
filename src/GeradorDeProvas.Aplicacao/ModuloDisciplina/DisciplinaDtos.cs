namespace GeradorDeProvas.Aplicacao.ModuloDisciplina;

public record ListarDisciplinaDto(
    Guid Id,
    string Nome
);

public record CadastrarDisciplinaDto(string Nome);

public record EditarDisciplinaDto(
    Guid Id,
    string Nome
);

public record DetalhesDisciplinaDto(
    Guid Id,
    string Nome
);
