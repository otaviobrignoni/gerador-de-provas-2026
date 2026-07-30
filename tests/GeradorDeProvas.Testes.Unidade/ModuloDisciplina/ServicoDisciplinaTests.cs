using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Testes.Unidade.Compartilhado;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.ModuloDisciplina;

[TestClass]
public sealed class ServicoDisciplinaTests
{
    [TestMethod]
    public void Cadastrar_DadosValidos_PersisteDisciplina()
    {
        // Arrange
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        Disciplina? disciplinaCadastrada = null;
        repositorioDisciplina.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.Cadastrar(It.IsAny<Disciplina>()))
            .Callback<Disciplina>(disciplina => disciplinaCadastrada = disciplina);
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Cadastrar(new CadastrarDisciplinaDto("Matemática"));

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(disciplinaCadastrada);
        Assert.AreEqual("Matemática", disciplinaCadastrada.Nome);
        repositorioDisciplina.Verify(r => r.Cadastrar(It.IsAny<Disciplina>()), Times.Once);
    }

    [TestMethod]
    public void Cadastrar_NomeDuplicado_RetornaFalha()
    {
        // Arrange
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        repositorioDisciplina.ConfigurarSelecao(new Disciplina("Matemática"));
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Cadastrar(new CadastrarDisciplinaDto(" MATEMÁTICA "));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarDisciplinaDto.Nome), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("Já existe", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Cadastrar(It.IsAny<Disciplina>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosValidos_AtualizaDisciplina()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        Disciplina? disciplinaAtualizada = null;
        repositorioDisciplina.ConfigurarSelecao(disciplina);
        repositorioDisciplina.Setup(r => r.Editar(disciplina.Id, It.IsAny<Disciplina>()))
            .Callback<Guid, Disciplina>((_, disciplina) => disciplinaAtualizada = disciplina)
            .Returns(true);
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Editar(new EditarDisciplinaDto(disciplina.Id, "Física"));

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(disciplinaAtualizada);
        Assert.AreEqual("Física", disciplinaAtualizada.Nome);
        repositorioDisciplina.Verify(r => r.Editar(disciplina.Id, It.IsAny<Disciplina>()), Times.Once);
    }

    [TestMethod]
    public void Editar_NomeDuplicado_RetornaFalha()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var outraDisciplina = new Disciplina("Física");
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        repositorioDisciplina.ConfigurarSelecao(disciplina, outraDisciplina);
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Editar(new EditarDisciplinaDto(disciplina.Id, " FÍSICA "));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(EditarDisciplinaDto.Nome), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("Já existe", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Disciplina>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DisciplinaInexistente_RetornaFalha()
    {
        // Arrange
        var disciplinaId = Guid.NewGuid();
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        repositorioDisciplina.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.Editar(disciplinaId, It.IsAny<Disciplina>())).Returns(false);
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Editar(new EditarDisciplinaDto(disciplinaId, "Matemática"));

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Editar(disciplinaId, It.IsAny<Disciplina>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_DisciplinaSemVinculos_ExcluiDisciplina()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao();
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Excluir(disciplina.Id);

        // Assert
        Assert.IsTrue(resultado.IsSuccess);
        repositorioDisciplina.Verify(r => r.Excluir(disciplina.Id), Times.Once);
    }

    [TestMethod]
    public void Excluir_DisciplinaComMateriasVinculadas_RetornaFalha()
    {
        // Arrange
        var disciplina = new Disciplina("Matemática");
        var repositorioDisciplina = new Mock<IRepositorioDisciplina>();
        var repositorioMateria = new Mock<IRepositorioMateria>();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao(new Materia("Álgebra", 7, disciplina));
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        // Act
        Result resultado = servicoDisciplina.Excluir(disciplina.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("matérias vinculadas", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Excluir(disciplina.Id), Times.Never);
    }
}
