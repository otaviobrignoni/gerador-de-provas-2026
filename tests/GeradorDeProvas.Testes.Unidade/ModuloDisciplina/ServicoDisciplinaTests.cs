using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
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
        var dto = CriarDtoCadastro();
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        Disciplina? disciplinaCadastrada = null;
        repositorioDisciplina.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.Cadastrar(It.IsAny<Disciplina>()))
            .Callback<Disciplina>(disciplina => disciplinaCadastrada = disciplina);

        // Act
        Result resultado = servicoDisciplina.Cadastrar(dto);

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
        var dto = CriarDtoCadastro(" MATEMÁTICA ");
        var disciplinaExistente = CriarDisciplina();
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao(disciplinaExistente);

        // Act
        Result resultado = servicoDisciplina.Cadastrar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarDisciplinaDto.Nome), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("Já existe", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Cadastrar(It.IsAny<Disciplina>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_NomeInvalido_RetornaFalha()
    {
        var dto = CriarDtoCadastro(" ");
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao();

        Result resultado = servicoDisciplina.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Nome", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Cadastrar(It.IsAny<Disciplina>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosValidos_AtualizaDisciplina()
    {
        // Arrange
        var disciplina = CriarDisciplina();
        var dto = CriarDtoEdicao(disciplina.Id, "Física");
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        Disciplina? disciplinaAtualizada = null;
        repositorioDisciplina.ConfigurarSelecao(disciplina);
        repositorioDisciplina.Setup(r => r.Editar(disciplina.Id, It.IsAny<Disciplina>()))
            .Callback<Guid, Disciplina>((_, disciplina) => disciplinaAtualizada = disciplina)
            .Returns(true);

        // Act
        Result resultado = servicoDisciplina.Editar(dto);

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
        var disciplina = CriarDisciplina();
        var outraDisciplina = CriarDisciplina("Física");
        var dto = CriarDtoEdicao(disciplina.Id, " FÍSICA ");
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao(disciplina, outraDisciplina);

        // Act
        Result resultado = servicoDisciplina.Editar(dto);

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
        var disciplinaId = Guid.CreateVersion7();
        var dto = CriarDtoEdicao(disciplinaId);
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.Editar(disciplinaId, It.IsAny<Disciplina>())).Returns(false);

        // Act
        Result resultado = servicoDisciplina.Editar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Editar(disciplinaId, It.IsAny<Disciplina>()), Times.Once);
    }

    [TestMethod]
    public void Editar_NomeInvalido_RetornaFalha()
    {
        var dto = CriarDtoEdicao(Guid.CreateVersion7(), "A");
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao();

        Result resultado = servicoDisciplina.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("mínimo", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Disciplina>()), Times.Never);
    }

    [TestMethod]
    public void Excluir_DisciplinaSemVinculos_ExcluiDisciplina()
    {
        // Arrange
        var disciplina = CriarDisciplina();
        var (repositorioDisciplina, repositorioMateria, servicoDisciplina) = CriarServico();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao();

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
        var disciplina = CriarDisciplina();
        var materia = new Materia("Álgebra", 7, disciplina);
        var (repositorioDisciplina, repositorioMateria, servicoDisciplina) = CriarServico();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao(materia);

        // Act
        Result resultado = servicoDisciplina.Excluir(disciplina.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("matérias vinculadas", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Excluir(disciplina.Id), Times.Never);
    }

    [TestMethod]
    public void Excluir_DisciplinaVinculadaDiretamenteAProva_RetornaFalhaAmigavelAntesDeExcluir()
    {
        var disciplina = CriarDisciplina();
        var materia = new Materia("Álgebra", 7, disciplina);
        var prova = new Prova("Avaliação de Matemática", disciplina, materia, 7, 1, false);
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioProva> repositorioProva = new();
        var servico = new ServicoDisciplina(
            repositorioDisciplina.Object,
            repositorioMateria.Object,
            repositorioProva.Object
        );
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioProva.ConfigurarSelecao(prova);

        Result resultado = servico.Excluir(disciplina.Id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            "Não é possível excluir esta disciplina, pois ela está vinculada a uma prova.",
            resultado.Errors.Single().Message
        );
        repositorioDisciplina.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void Excluir_DisciplinaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(id)).Returns((Disciplina?)null);

        Result resultado = servicoDisciplina.Excluir(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
        repositorioDisciplina.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void SelecionarTodos_DisciplinasExistentes_RetornaDtosMapeados()
    {
        var primeira = CriarDisciplina();
        var segunda = CriarDisciplina("Física");
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao(primeira, segunda);

        List<ListarDisciplinaDto> resultado = servicoDisciplina.SelecionarTodos();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(primeira.Id, resultado[0].Id);
        Assert.AreEqual("Matemática", resultado[0].Nome);
        Assert.AreEqual(segunda.Id, resultado[1].Id);
    }

    [TestMethod]
    public void SelecionarPorId_DisciplinaExistente_RetornaDetalhesMapeados()
    {
        var disciplina = CriarDisciplina();
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);

        Result<DetalhesDisciplinaDto> resultado = servicoDisciplina.SelecionarPorId(disciplina.Id);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(disciplina.Id, resultado.Value.Id);
        Assert.AreEqual(disciplina.Nome, resultado.Value.Nome);
    }

    [TestMethod]
    public void SelecionarPorId_DisciplinaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioDisciplina, _, servicoDisciplina) = CriarServico();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(id)).Returns((Disciplina?)null);

        Result<DetalhesDisciplinaDto> resultado = servicoDisciplina.SelecionarPorId(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
    }

    private static Disciplina CriarDisciplina(string nome = "Matemática")
    {
        return new Disciplina(nome);
    }

    private static CadastrarDisciplinaDto CriarDtoCadastro(string nome = "Matemática")
    {
        return new CadastrarDisciplinaDto(nome);
    }

    private static EditarDisciplinaDto CriarDtoEdicao(Guid id, string nome = "Matemática")
    {
        return new EditarDisciplinaDto(id, nome);
    }

    private static (Mock<IRepositorioDisciplina> repositorioDisciplina, Mock<IRepositorioMateria> repositorioMateria, ServicoDisciplina servicoDisciplina) CriarServico()
    {
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        ServicoDisciplina servicoDisciplina = new(repositorioDisciplina.Object, repositorioMateria.Object);

        return (repositorioDisciplina, repositorioMateria, servicoDisciplina);
    }
}
