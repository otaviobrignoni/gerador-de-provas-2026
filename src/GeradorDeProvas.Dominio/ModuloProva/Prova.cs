using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Dominio.ModuloProva;

public sealed class Prova : EntidadeBase<Prova>, IEntidadeDoUsuario
{
    public string Titulo { get; set; } = string.Empty;
    public Disciplina Disciplina { get; set; } = null!;
    public Materia? Materia { get; set; }
    public int Serie { get; set; }
    public int QuantidadeQuestoes { get; set; }
    public bool ProvaRecuperacao { get; set; }
    public List<Questao> Questoes { get; set; } = [];
    public Guid UserId { get; set; }

    public Prova() { }

    public Prova(string titulo, Disciplina disciplina, Materia? materia, int serie, int quantidadeQuestoes, bool provaRecuperacao, List<Questao>? questoes = null) : this()
    {
        Titulo = titulo;
        Disciplina = disciplina;
        Materia = materia;
        Serie = serie;
        QuantidadeQuestoes = quantidadeQuestoes;
        ProvaRecuperacao = provaRecuperacao;
        Questoes = questoes ?? [];
    }

    public override List<string> Validar()
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(Titulo) || Titulo.Length < 2 || Titulo.Length > 100)
            erros.Add("O campo \"Título\" deve ser conter entre 2 e 100 caracteres.");

        if (Disciplina is null)
            erros.Add("O campo \"Disciplina\" deve ser preenchido.");

        if (Serie <= 0)
            erros.Add("O campo \"Série\" deve ser maior que zero.");

        if (ProvaRecuperacao && Materia is not null)
            erros.Add("O campo \"Matéria\" não pode ser prenchido em uma prova de recuperação.");

        else if (!ProvaRecuperacao && Materia is null)
            erros.Add("O campo \"Matéria\" deve ser preenchido.");

        else if (!ProvaRecuperacao && Materia is not null && !Equals(Materia.Serie, Serie))
            erros.Add("O campo \"Série\" precisa alinhar com a série da \"Matéria\".");

        if (QuantidadeQuestoes < 1)
            erros.Add("O campo \"Quantidade de Questões\" não pode ser zero ou negativo.");

        if (!ProvaRecuperacao && Materia is not null && !Equals(Disciplina, Materia.Disciplina))
            erros.Add("O valor do campo \"Matéria\" deve pertencer à \"Disciplina\" selecionada.");

        return erros;
    }

    public override void Atualizar(Prova entidadeAtualizada)
    {
        Titulo = entidadeAtualizada.Titulo;
        Disciplina = entidadeAtualizada.Disciplina;
        Materia = entidadeAtualizada.Materia;
        Serie = entidadeAtualizada.Serie;
        QuantidadeQuestoes = entidadeAtualizada.QuantidadeQuestoes;
        ProvaRecuperacao = entidadeAtualizada.ProvaRecuperacao;

        Questoes.Clear();
    }

    public List<string> SortearQuestoes(IEnumerable<Questao> questoesDisponiveis, int? seed = null)
    {
        List<string> erros = [];

        List<Questao> questoesDisponiveisDistintas = [.. questoesDisponiveis.DistinctBy(q => q.Id)];

        if (QuantidadeQuestoes < 1)
            erros.Add("A quantidade de questões deve ser maior que zero.");

        if (questoesDisponiveisDistintas.Count < QuantidadeQuestoes)
            erros.Add("A quantidade de questões informada é maior que a quantidade disponível.");

        Random gerador = seed is not null ? new Random(seed.Value) : Random.Shared;

        Questoes = [.. questoesDisponiveisDistintas
            .OrderBy(_ => gerador.Next())
            .Take(QuantidadeQuestoes)
        ];

        return erros;
    }
}
