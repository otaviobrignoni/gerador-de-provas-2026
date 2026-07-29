using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.Compartilhado.Identity;

namespace GeradorDeProvas.Dominio.ModuloQuestao;

public class Alternativa : EntidadeBase<Alternativa>, IEntidadeDoUsuario
{
    public string Texto { get; set; } = string.Empty;
    public bool Correta { get; set; }
    public Questao Questao { get; set; } = null!;
    public Guid UserId { get; set; }

    public Alternativa()
    {
    }

    public Alternativa(string texto, bool correta) : this()
    {
        Texto = texto;
        Correta = correta;
    }

    public override List<string> Validar()
    {
        List<string> erros = [];

        if (string.IsNullOrWhiteSpace(Texto) || Texto.Length > 1000)
            erros.Add("O campo \"Texto\" da alternativa deve ser preenchido e conter no máximo 1000 caracteres.");

        if (Questao is null)
            erros.Add("A alternativa deve estar vinculada a uma questão.");

        return erros;
    }

    public override void Atualizar(Alternativa entidadeAtualizada)
    {
        Texto = entidadeAtualizada.Texto;
        Correta = entidadeAtualizada.Correta;
    }
}
