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
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Questao primeiraQuestao = new("Quanto é 2 + 2?", materia, [new Alternativa("4", true), new Alternativa("5", false)]);
        Questao segundaQuestao = new("Quanto é 3 + 3?", materia, [new Alternativa("6", true), new Alternativa("7", false)]);
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Prova? provaCadastrada = null;
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioQuestao.ConfigurarSelecao(primeiraQuestao, segundaQuestao);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>()))
            .Callback<Prova>(prova => provaCadastrada = prova);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);
        CadastrarProvaDto dto = new("Avaliação de Álgebra", disciplina.Id, materia.Id, 7, 2, false);
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
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Prova provaExistente = new("Avaliação de Álgebra", disciplina, materia, 7, 2, false);
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioProva.ConfigurarSelecao(provaExistente);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);
        CadastrarProvaDto dto = new(" AVALIAÇÃO DE ÁLGEBRA ", disciplina.Id, materia.Id, 7, 2, false);

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
        Disciplina disciplina = new("Matemática");
        Disciplina outraDisciplina = new("Geografia");
        Materia materia = new("Relevo", 7, outraDisciplina);
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);
        CadastrarProvaDto dto = new("Avaliação de Matemática", disciplina.Id, materia.Id, 7, 1, false);

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
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Materia outraMateria = new("Geometria", 7, disciplina);
        Questao questaoForaDaConfiguracao = new("Qual é a área de um quadrado?", outraMateria, [
            new Alternativa("Lado ao quadrado", true),
            new Alternativa("Base vezes altura", false)
        ]);
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia, outraMateria);
        repositorioQuestao.ConfigurarSelecao(questaoForaDaConfiguracao);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);
        CadastrarProvaDto dto = new("Avaliação de Álgebra", disciplina.Id, materia.Id, 7, 1, false);

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
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Questao questao = new("Quanto é 2 + 2?", materia, [new Alternativa("4", true), new Alternativa("5", false)]);
        Prova provaOriginal = new("Avaliação de Álgebra", disciplina, materia, 7, 2, false, [questao]);
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Prova? provaDuplicada = null;
        repositorioProva.ConfigurarSelecao(provaOriginal);
        repositorioProva.Setup(r => r.SelecionarPorId(provaOriginal.Id)).Returns(provaOriginal);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>()))
            .Callback<Prova>(prova => provaDuplicada = prova);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoProva.Duplicar(new DuplicarProvaDto(provaOriginal.Id, "Segunda Avaliação de Álgebra"));

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
        Mock<IRepositorioProva> repositorioProva = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioProva.Setup(r => r.Excluir(provaId)).Returns(false);
        ServicoProva servicoProva = new(repositorioProva.Object, repositorioDisciplina.Object, repositorioMateria.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoProva.Excluir(provaId);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Prova não encontrada", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Excluir(provaId), Times.Once);
    }
}
