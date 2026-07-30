using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloProva;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Testes.Unidade.Compartilhado;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.ModuloProva;

[TestClass]
public sealed class ServicoProvaTests
{
    [TestMethod]
    public void Cadastrar_ConfiguracaoValida_CadastraProvaComQuestoesSelecionadas()
    {
        // Arrange
        var (disciplina, materia, questoes, _) = CriarProva(2);
        var primeiraQuestao = questoes[0];
        var segundaQuestao = questoes[1];
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        Prova? provaCadastrada = null;
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioQuestao.ConfigurarSelecao(primeiraQuestao, segundaQuestao);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>()))
            .Callback<Prova>(prova => provaCadastrada = prova);
        var dto = CriarDtoCadastro(disciplina, materia);
        List<Guid> questaoIds = [segundaQuestao.Id, primeiraQuestao.Id];

        // Act
        Result resultado = servicoProva.Cadastrar(dto, questaoIds);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(provaCadastrada);
        Assert.AreEqual("Avaliação de Álgebra", provaCadastrada.Titulo);
        Assert.AreSame(disciplina, provaCadastrada.Disciplina);
        Assert.AreSame(materia, provaCadastrada.Materia);
        Assert.AreEqual(7, provaCadastrada.Serie);
        Assert.AreEqual(2, provaCadastrada.QuantidadeQuestoes);
        Assert.IsFalse(provaCadastrada.ProvaRecuperacao);
        CollectionAssert.AreEqual(questaoIds, provaCadastrada.Questoes.Select(q => q.Id).ToList());
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Once);
    }

    [TestMethod]
    public void Cadastrar_TituloDuplicado_RetornaFalha()
    {
        // Arrange
        var (disciplina, materia, _, provaExistente) = CriarProva(0);
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao(provaExistente);
        var dto = CriarDtoCadastro(disciplina, materia, " AVALIAÇÃO DE ÁLGEBRA ");

        // Act
        Result resultado = servicoProva.Cadastrar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.Titulo), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("Já existe", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_MateriaDeOutraDisciplina_RetornaFalha()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var outraDisciplina = new Disciplina("Geografia");
        var materia = new Materia("Relevo", 7, outraDisciplina);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        var dto = CriarDtoCadastro(disciplina, materia, "Avaliação de Matemática", 1);

        // Act
        Result resultado = servicoProva.Cadastrar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.MateriaId), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("não pertence à disciplina", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_QuestoesForaDaConfiguracao_RetornaFalha()
    {
        // Arrange
        var (disciplina, materia, _, _) = CriarProva(0);
        var outraMateria = new Materia("Geometria", 7, disciplina);
        var questaoForaDaConfiguracao = new Questao(
            "Qual é a área de um quadrado?",
            outraMateria,
            CriarAlternativas("Lado ao quadrado", "Base vezes altura")
        );
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia, outraMateria);
        repositorioQuestao.ConfigurarSelecao(questaoForaDaConfiguracao);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 1);

        // Act
        Result resultado = servicoProva.Cadastrar(dto, [questaoForaDaConfiguracao.Id]);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.QuantidadeQuestoes), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("não pertencem à configuração", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Duplicar_ProvaExistente_CadastraCopiaComNovoTitulo()
    {
        // Arrange
        var (disciplina, materia, _, provaOriginal) = CriarProva();
        var dto = CriarDtoDuplicacao(provaOriginal);
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        Prova? provaDuplicada = null;
        repositorioProva.ConfigurarSelecao(provaOriginal);
        repositorioProva.Setup(r => r.SelecionarPorId(provaOriginal.Id)).Returns(provaOriginal);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>()))
            .Callback<Prova>(prova => provaDuplicada = prova);

        // Act
        Result resultado = servicoProva.Duplicar(dto);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(provaDuplicada);
        Assert.AreNotSame(provaOriginal, provaDuplicada);
        Assert.AreNotEqual(provaOriginal.Id, provaDuplicada.Id);
        Assert.AreEqual("Segunda Avaliação de Álgebra", provaDuplicada.Titulo);
        Assert.AreSame(disciplina, provaDuplicada.Disciplina);
        Assert.AreSame(materia, provaDuplicada.Materia);
        Assert.AreEqual(7, provaDuplicada.Serie);
        Assert.AreEqual(2, provaDuplicada.QuantidadeQuestoes);
        Assert.IsFalse(provaDuplicada.ProvaRecuperacao);
        Assert.IsEmpty(provaDuplicada.Questoes);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_ProvaInexistente_RetornaFalha()
    {
        // Arrange
        Guid provaId = Guid.CreateVersion7();
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.Setup(r => r.Excluir(provaId)).Returns(false);

        // Act
        Result resultado = servicoProva.Excluir(provaId);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Prova não encontrada", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Excluir(provaId), Times.Once);
    }

    private static (Disciplina disciplina, Materia materia, List<Questao> questoes, Prova prova) CriarProva(int quantidadeQuestoesCriadas = 1)
    {
        if (quantidadeQuestoesCriadas < 0)
            throw new ArgumentOutOfRangeException(nameof(quantidadeQuestoesCriadas), "A quantidade deve ser maior ou igual a zero.");

        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 7, disciplina);
        var questoes = Enumerable
            .Range(1, quantidadeQuestoesCriadas)
            .Select(i => new Questao($"Quanto é {i + 1} + {i + 1}?", materia, CriarAlternativas($"{(i + 1) * 2}", $"{(i + 1) * 2 + 1}")))
            .ToList();
        var prova = new Prova("Avaliação de Álgebra", disciplina, materia, 7, 2, false, questoes);

        return (disciplina, materia, questoes, prova);
    }

    private static List<Alternativa> CriarAlternativas(string primeiroTexto = "4", string segundoTexto = "5", bool primeiraCorreta = true)
    {
        return [new Alternativa(primeiroTexto, primeiraCorreta), new Alternativa(segundoTexto, !primeiraCorreta)];
    }

    private static CadastrarProvaDto CriarDtoCadastro(Disciplina disciplina, Materia materia, string titulo = "Avaliação de Álgebra", int quantidadeQuestoes = 2)
    {
        return new CadastrarProvaDto(titulo, disciplina.Id, materia.Id, 7, quantidadeQuestoes, false);
    }

    private static DuplicarProvaDto CriarDtoDuplicacao(Prova prova)
    {
        return new DuplicarProvaDto(prova.Id, "Segunda Avaliação de Álgebra");
    }

    private static (Mock<IRepositorioProva> repositorioProva, Mock<IRepositorioDisciplina> repositorioDisciplina, Mock<IRepositorioMateria> repositorioMateria, Mock<IRepositorioQuestao> repositorioQuestao, ServicoProva servicoProva) CriarServico()
    {
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);

        return (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva);
    }
}
