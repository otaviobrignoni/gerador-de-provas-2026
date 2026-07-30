using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Testes.Unidade.Compartilhado;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.ModuloMateria;

[TestClass]
public sealed class ServicoMateriaTests
{
    [TestMethod]
    public void Cadastrar_DadosValidos_PersisteMateria()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Materia? materiaCadastrada = null;
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.Cadastrar(It.IsAny<Materia>())).Callback<Materia>(materia => materiaCadastrada = materia);
        ServicoMateria servicoMateria = new(repositorioMateria.Object, repositorioDisciplina.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoMateria.Cadastrar(new CadastrarMateriaDto("Álgebra", 7, disciplina.Id));

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(materiaCadastrada);
        Assert.AreEqual("Álgebra", materiaCadastrada.Nome);
        Assert.AreEqual(7, materiaCadastrada.Serie);
        Assert.AreSame(disciplina, materiaCadastrada.Disciplina);
        repositorioMateria.Verify(r => r.Cadastrar(It.IsAny<Materia>()), Times.Once);
    }

    [TestMethod]
    public void Cadastrar_MateriaCom_DisciplinaInexistente_RetornaFalha()
    {
        // Arrange
        Guid disciplinaId = Guid.CreateVersion7();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplinaId)).Returns((Disciplina?)null);
        ServicoMateria servicoMateria = new(repositorioMateria.Object, repositorioDisciplina.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoMateria
            .Cadastrar(new CadastrarMateriaDto("Álgebra", 7, disciplinaId));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual("DisciplinaId", resultado.Errors.Single().Metadata["Campo"]);
        repositorioMateria.Verify(r => r.Cadastrar(It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Editar_MateriaInexistente_RetornaFalha()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Guid materiaId = Guid.CreateVersion7();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.Editar(materiaId, It.IsAny<Materia>())).Returns(false);
        ServicoMateria servicoMateria = new(repositorioMateria.Object, repositorioDisciplina.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoMateria
            .Editar(new EditarMateriaDto(materiaId, "Álgebra", 7, disciplina.Id));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Matéria não encontrada", resultado.Errors.Single().Message);
        repositorioMateria.Verify(r => r.Editar(materiaId, It.IsAny<Materia>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_MateriaComQuestoesVinculadas_RetornaFalha()
    {
        // Arrange
        Disciplina disciplina = new("Matemática");
        Materia materia = new("Álgebra", 7, disciplina);
        Questao questao = new("Quanto é 2 + 2?", materia, [new Alternativa("4", true), new Alternativa("5", false)]);
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.ConfigurarSelecao(questao);
        ServicoMateria servicoMateria = new(repositorioMateria.Object, repositorioDisciplina.Object, repositorioQuestao.Object);

        // Act
        Result resultado = servicoMateria.Excluir(materia.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("questões vinculadas", resultado.Errors.Single().Message);
        repositorioMateria.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }
}
