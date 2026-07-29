using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;

namespace GeradorDeProvas.Dominio.ModuloQuestao;

public class Questao : EntidadeBase<Questao>, IEntidadeDoUsuario
{
    public string Enunciado { get; set; } = string.Empty;
    public Materia Materia { get; set; } = null!;
    public List<Alternativa> Alternativas { get; set; } = [];
    public List<Prova> Provas { get; set; } = [];
    public Guid UserId { get; set; }

    public Questao() { }

    public Questao(string enunciado, Materia materia, List<Alternativa> alternativas) : this()
    {
        Enunciado = enunciado;
        Materia = materia;
        Alternativas = alternativas;

        foreach (Alternativa alternativa in Alternativas)
            alternativa.Questao = this;
    }

    public override List<string> Validar()
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(Enunciado) || Enunciado.Length > 2000)
            erros.Add("O campo \"Enunciado\" deve ser preenchido e conter no máximo 2000 caracteres.");

        if (Materia is null)
            erros.Add("O campo \"Matéria\" deve ser preenchido.");

        if (Alternativas.Count < 2)
            erros.Add("A questão deve possuir no mínimo duas alternativas.");
        else if (Alternativas.Count > 4)
            erros.Add("A questão deve possuir no máximo quatro alternativas.");

        int quantidadeCorretas = Alternativas.Count(a => a.Correta);

        if (quantidadeCorretas == 0)
            erros.Add("A questão deve possuir uma alternativa correta.");
        else if (quantidadeCorretas > 1)
            erros.Add("A questão deve possuir apenas uma alternativa correta.");

        foreach (Alternativa alternativa in Alternativas)
            erros.AddRange(alternativa.Validar());

        return erros;
    }

    public override void Atualizar(Questao entidadeAtualizada)
    {
        Enunciado = entidadeAtualizada.Enunciado;
        Materia = entidadeAtualizada.Materia;

        Alternativas.Clear();

        foreach (Alternativa alternativa in entidadeAtualizada.Alternativas)
        {
            alternativa.Questao = this;
            Alternativas.Add(alternativa);
        }
    }
}
