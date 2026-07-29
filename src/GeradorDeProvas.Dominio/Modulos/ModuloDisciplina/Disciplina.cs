using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.Compartilhado.Identity;
using GeradorDeProvas.Dominio.Modulos.ModuloMateria;

namespace GeradorDeProvas.Dominio.Modulos.ModuloDisciplina;

public class Disciplina : EntidadeBase<Disciplina>, IEntidadeDoUsuario
{
    public string Nome { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public List<Materia> Materias { get; set; } = [];

    public Disciplina()
    {
    }

    public Disciplina(string nome) : this()
    {
        Nome = nome;
    }

    public override List<string> Validar()
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(Nome))
            erros.Add("O campo \"Nome\" deve ser preenchido.");

        else if (Nome.Length < 2)
            erros.Add("O campo \"Nome\" deve conter no mínimo 2 caracteres.");

        else if (Nome.Length > 100)
            erros.Add("O campo \"Nome\" deve conter no máximo 100 caracteres.");

        return erros;
    }

    public override void Atualizar(Disciplina entidadeAtualizada)
    {
        Nome = entidadeAtualizada.Nome;
    }
}
