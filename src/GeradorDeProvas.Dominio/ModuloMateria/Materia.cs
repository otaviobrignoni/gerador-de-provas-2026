using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Dominio.ModuloMateria;

public class Materia : EntidadeBase<Materia>, IEntidadeDoUsuario
{
    public string Nome { get; set; } = string.Empty;
    public int Serie { get; set; }
    public Disciplina Disciplina { get; set; } = null!;
    public List<Questao> Questoes { get; set; } = [];
    public List<Prova> Provas { get; set; } = [];
    public Guid UserId { get; set; }

    public Materia()
    {
    }

    public Materia(string nome, int serie, Disciplina disciplina) : this()
    {
        Nome = nome;
        Serie = serie;
        Disciplina = disciplina;
    }

    public override List<string> Validar()
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(Nome) || Nome.Length < 2 || Nome.Length > 100)
            erros.Add("O campo \"Nome\" deve conter entre 2 e 100 caracteres.");

        if (Serie <= 0)
            erros.Add("O campo \"Série\" deve ser preenchido.");

        if (Disciplina is null)
            erros.Add("O campo \"Disciplina\" deve ser preenchido.");

        return erros;
    }

    public override void Atualizar(Materia entidadeAtualizada)
    {
        Nome = entidadeAtualizada.Nome;
        Serie = entidadeAtualizada.Serie;
        Disciplina = entidadeAtualizada.Disciplina;
    }
}
