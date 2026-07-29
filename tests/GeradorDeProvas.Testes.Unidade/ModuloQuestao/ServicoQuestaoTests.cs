using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloQuestao;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.ModuloQuestao;

[TestClass]
public sealed class ServicoQuestaoTests
{
    [TestMethod]
    public void Cadastrar_DadosValidos_CadastraQuestaoComAlternativas()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        Questao? questaoCadastrada = null;
        repositorioQuestao.Setup(r => r.Cadastrar(It.IsAny<Questao>())).Callback<Questao>(questao => questaoCadastrada = questao);
        ServicoQuestao servicoQuestao = new(repositorioQuestao.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoQuestao.Cadastrar(new CadastrarQuestaoDto("Quanto é 2 + 2?", materia.Id, [new CadastrarAlternativaDto("4", true), new CadastrarAlternativaDto("5", false)]));

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(questaoCadastrada);
        Assert.AreEqual("Quanto é 2 + 2?", questaoCadastrada.Enunciado);
        Assert.AreSame(materia, questaoCadastrada.Materia);
        Assert.HasCount(2, questaoCadastrada.Alternativas);
        repositorioQuestao.Verify(r => r.Cadastrar(It.IsAny<Questao>()), Times.Once);
    }

    [TestMethod]
    public void Cadastrar_MateriaInexistente_RetornaFalha()
    {
        // Arrange
        Guid materiaId = Guid.CreateVersion7();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        repositorioMateria.Setup(r => r.SelecionarPorId(materiaId)).Returns((Materia?)null);
        ServicoQuestao servicoQuestao = new(repositorioQuestao.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoQuestao.Cadastrar(new CadastrarQuestaoDto("Quanto é 2 + 2?", materiaId, [new CadastrarAlternativaDto("4", true), new CadastrarAlternativaDto("5", false)]));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual("MateriaId", resultado.Errors.Single().Metadata["Campo"]);
        repositorioQuestao.Verify(r => r.Cadastrar(It.IsAny<Questao>()), Times.Never);
    }

    [TestMethod]
    public void Editar_QuestaoInexistente_RetornaFalha()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Guid questaoId = Guid.CreateVersion7();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.Setup(r => r.Editar(questaoId, It.IsAny<Questao>())).Returns(false);
        ServicoQuestao servicoQuestao = new(repositorioQuestao.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoQuestao.Editar(new EditarQuestaoDto(questaoId, "Quanto é 2 + 2?", materia.Id, [new CadastrarAlternativaDto("4", true), new CadastrarAlternativaDto("5", false)]));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Questão não encontrada", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Editar(questaoId, It.IsAny<Questao>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_QuestaoVinculadaAProva_RetornaFalha()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Questao questao = new("Quanto é 2 + 2?", materia, [new Alternativa("4", true), new Alternativa("5", false)]);
        Prova prova = new("Avaliação", disciplina, materia, 7, 1, false, [questao]);
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioProva> repositorioProva = new();
        repositorioQuestao.Setup(r => r.SelecionarPorId(questao.Id)).Returns(questao);
        repositorioProva.Setup(r => r.SelecionarTodos()).Returns([prova]);
        ServicoQuestao servico = new(repositorioQuestao.Object, repositorioMateria.Object, repositorioProva.Object);

        // Act
        Result resultado = servico.Excluir(questao.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("vinculada a uma prova", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }
}
