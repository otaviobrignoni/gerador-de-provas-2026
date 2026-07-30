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
        var (disciplina, _) = CriarMateria();
        var dto = CriarDtoCadastro(disciplina.Id);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        Materia? materiaCadastrada = null;
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.Cadastrar(It.IsAny<Materia>())).Callback<Materia>(materia => materiaCadastrada = materia);

        // Act
        Result resultado = servicoMateria.Cadastrar(dto);

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
        var dto = CriarDtoCadastro(disciplinaId);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplinaId)).Returns((Disciplina?)null);

        // Act
        Result resultado = servicoMateria.Cadastrar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual("DisciplinaId", resultado.Errors.Single().Metadata["Campo"]);
        repositorioMateria.Verify(r => r.Cadastrar(It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Editar_MateriaInexistente_RetornaFalha()
    {
        // Arrange
        var (disciplina, _) = CriarMateria();
        Guid materiaId = Guid.CreateVersion7();
        var dto = CriarDtoEdicao(materiaId, disciplina.Id);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.Editar(materiaId, It.IsAny<Materia>())).Returns(false);

        // Act
        Result resultado = servicoMateria.Editar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Matéria não encontrada", resultado.Errors.Single().Message);
        repositorioMateria.Verify(r => r.Editar(materiaId, It.IsAny<Materia>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_MateriaComQuestoesVinculadas_RetornaFalha()
    {
        // Arrange
        var (_, materia) = CriarMateria();
        var questao = new Questao("Quanto é 2 + 2?", materia, [new Alternativa("4", true), new Alternativa("5", false)]);
        var (repositorioMateria, _, repositorioQuestao, servicoMateria) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.ConfigurarSelecao(questao);

        // Act
        Result resultado = servicoMateria.Excluir(materia.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("questões vinculadas", resultado.Errors.Single().Message);
        repositorioMateria.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    private static (Disciplina disciplina, Materia materia) CriarMateria()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 7, disciplina);

        return (disciplina, materia);
    }

    private static CadastrarMateriaDto CriarDtoCadastro(Guid disciplinaId)
    {
        return new CadastrarMateriaDto("Álgebra", 7, disciplinaId);
    }

    private static EditarMateriaDto CriarDtoEdicao(Guid id, Guid disciplinaId)
    {
        return new EditarMateriaDto(id, "Álgebra", 7, disciplinaId);
    }

    private static (Mock<IRepositorioMateria> repositorioMateria, Mock<IRepositorioDisciplina> repositorioDisciplina, Mock<IRepositorioQuestao> repositorioQuestao, ServicoMateria servicoMateria) CriarServico()
    {
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        ServicoMateria servicoMateria = new(repositorioMateria.Object, repositorioDisciplina.Object, repositorioQuestao.Object);

        return (repositorioMateria, repositorioDisciplina, repositorioQuestao, servicoMateria);
    }
}
