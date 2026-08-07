using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;

namespace GeradorDeProvas.Testes.Unidade.ModuloProva;

[TestClass]
public sealed class ProvaTests
{
    [TestMethod]
    public void Validar_SemTitulo_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova(string.Empty, disciplina, materia, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Título\" deve ser conter entre 2 e 100 caracteres.", erros.First());
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(101)]
    public void Validar_ComTituloForaDosLimites_DeveRetornar_ErroCorrespondente(int quantidadeCaracteres)
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova(new string('A', quantidadeCaracteres), disciplina, materia, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Título\" deve ser conter entre 2 e 100 caracteres.", erros.First());
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(100)]
    public void Validar_ComTituloNosLimites_NaoDeveRetornarErros(int quantidadeCaracteres)
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova(new string('A', quantidadeCaracteres), disciplina, materia, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Validar_SemDisciplina_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var materia = new Materia("Álgebra", 8, null!);

        var prova = new Prova("Prova de Álgebra 8a Serie", null!, materia, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Disciplina\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ComSerieZeroOuAbaixo_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 0, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 0, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" deve ser maior que zero.", erros.First());
    }

    [TestMethod]
    public void Validar_ComSerieNegativa_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var prova = new Prova("Recuperação de Matemática", disciplina, null, -1, 1, true);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" deve ser maior que zero.", erros.First());
    }

    [TestMethod]
    public void Validar_ComSerieEMateria_Diferentes_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 5, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Série\" precisa alinhar com a série da \"Matéria\".", erros.First());
    }

    [TestMethod]
    public void Validar_RecuperacaoComMateria_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 1, true);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Matéria\" não pode ser prenchido em uma prova de recuperação.", erros.First());
    }

    [TestMethod]
    public void Validar_ProvaComumSemMateria_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var prova = new Prova("Prova de Matemática", disciplina, null, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Matéria\" deve ser preenchido.", erros.First());
    }

    [TestMethod]
    public void Validar_ProvaRecuperacaoSemMateria_NaoDeveRetornarErros()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var prova = new Prova("Recuperação de Matemática", disciplina, null, 8, 1, true);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Validar_QuantidadeQuestoesAbaixoDeUm_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 0, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Quantidade de Questões\" não pode ser zero ou negativo.", erros.First());
    }

    [TestMethod]
    public void Validar_ComQuantidadeQuestoesNegativa_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, -1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O campo \"Quantidade de Questões\" não pode ser zero ou negativo.", erros.First());
    }

    [TestMethod]
    public void Validar_QuantidadeAcimaDoLimitePraticoDoFluxo_RetornaErro()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova(
            "Prova extensa",
            disciplina,
            materia,
            8,
            Prova.QuantidadeMaximaQuestoes + 1,
            false
        );

        List<string> erros = prova.Validar();

        Assert.HasCount(1, erros);
        Assert.AreEqual("A prova deve possuir no máximo 60 questões.", erros.Single());
    }

    [TestMethod]
    public void Validar_ProvaComumComDadosValidos_NaoDeveRetornarErros()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 1, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.IsEmpty(erros);
    }

    [TestMethod]
    public void Validar_MateriaFora_DaDisciplina_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");

        var disciplina2 = new Disciplina("Geografia");
        var materia2 = new Materia("Relevo", 8, disciplina2);

        var prova = new Prova("Prova de Álgebra", disciplina, materia2, 8, 3, false);

        // Act
        var erros = prova.Validar();

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("O valor do campo \"Matéria\" deve pertencer à \"Disciplina\" selecionada.", erros.First());
    }

    [TestMethod]
    public void Atualizar_AlteraConfiguracaoELimpaQuestoes()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 1, false);

        var disciplina2 = new Disciplina("Geografia");

        // Act
        prova.Atualizar(new Prova("Prova de Geografia", disciplina2, null, 6, 3, true));

        // Assert
        Assert.AreEqual("Prova de Geografia", prova.Titulo);
        Assert.AreEqual(6, prova.Serie);
        Assert.AreEqual(3, prova.QuantidadeQuestoes);
        Assert.IsTrue(prova.ProvaRecuperacao);
        Assert.IsNull(prova.Materia);
        Assert.HasCount(0, prova.Questoes);
    }

    [TestMethod]
    public void SortearQuestoes_DeveSelecionar_QuantidadeInformada_SemRepetir()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);

        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 5, false);

        List<Questao> questoesDisponiveis = [.. Enumerable
            .Range(1, 10)
            .Select(indice => new Questao($"Questão {indice}", materia, []))
        ];

        // Act
        var erros = prova.SortearQuestoes(questoesDisponiveis, 1);

        // Assert
        Assert.IsEmpty(erros);
        Assert.HasCount(5, prova.Questoes);
        Assert.HasCount(5, prova.Questoes.Select(q => q.Id).Distinct());
        Assert.IsTrue(prova.Questoes.All(questoesDisponiveis.Contains));
    }

    [TestMethod]
    public void SortearQuestoes_ComQuestoesDuplicadas_DeveConsiderarApenasQuestoesDistintas()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 2, false);
        var primeiraQuestao = new Questao("Questão 1", materia, []);
        var segundaQuestao = new Questao("Questão 2", materia, []);
        List<Questao> questoesDisponiveis = [primeiraQuestao, primeiraQuestao, segundaQuestao];

        // Act
        var erros = prova.SortearQuestoes(questoesDisponiveis, 1);

        // Assert
        Assert.IsEmpty(erros);
        Assert.HasCount(2, prova.Questoes);
        Assert.HasCount(2, prova.Questoes.Select(q => q.Id).Distinct());
    }

    [TestMethod]
    public void SortearQuestoes_ComQuantidadeAbaixoDeUm_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 0, false);

        // Act
        var erros = prova.SortearQuestoes([]);

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A quantidade de questões deve ser maior que zero.", erros.First());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void SortearQuestoes_ComQuantidadeInvalida_NaoDeveAlterarQuestoes(int quantidadeQuestoes)
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var questaoOriginal = new Questao("Questão original", materia, []);
        var prova = new Prova(
            "Prova de Álgebra",
            disciplina,
            materia,
            8,
            quantidadeQuestoes,
            false,
            [questaoOriginal]
        );

        // Act
        _ = prova.SortearQuestoes([new Questao("Nova questão", materia, [])], 1);

        // Assert
        Assert.HasCount(1, prova.Questoes);
        Assert.AreSame(questaoOriginal, prova.Questoes.Single());
    }

    [TestMethod]
    public void SortearQuestoes_ComQuantidadeMaiorQueDisponivel_DeveRetornar_ErroCorrespondente()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 3, false);

        List<Questao> questoesDisponiveis = [
            new Questao("Quanto é 2 + 2?", materia, []),
            new Questao("Quanto é 3 + 3?", materia, [])
        ];

        // Act
        var erros = prova.SortearQuestoes(questoesDisponiveis);

        // Assert
        Assert.HasCount(1, erros);
        Assert.AreEqual("A quantidade de questões informada é maior que a quantidade disponível.", erros.First());
    }

    [TestMethod]
    public void SortearQuestoes_ComQuantidadeMaiorQueDisponivel_NaoDeveAlterarQuestoes()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 8, disciplina);
        var questaoOriginal = new Questao("Questão original", materia, []);
        var prova = new Prova("Prova de Álgebra", disciplina, materia, 8, 2, false, [questaoOriginal]);

        // Act
        _ = prova.SortearQuestoes([new Questao("Nova questão", materia, [])], 1);

        // Assert
        Assert.HasCount(1, prova.Questoes);
        Assert.AreSame(questaoOriginal, prova.Questoes.Single());
    }
}
