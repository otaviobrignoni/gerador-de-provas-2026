using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
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
    public void Cadastrar_DadosInvalidos_RetornaFalha()
    {
        var (disciplina, _) = CriarMateria();
        var dto = new CadastrarMateriaDto("A", 0, disciplina.Id);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);

        Result resultado = servicoMateria.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Nome", resultado.Errors.Single().Message);
        repositorioMateria.Verify(r => r.Cadastrar(It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_NomeDuplicado_RetornaFalha()
    {
        var (disciplina, materia) = CriarMateria();
        var dto = new CadastrarMateriaDto(" ÁLGEBRA ", 8, disciplina.Id);
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia);

        Result resultado = servicoMateria.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarMateriaDto.Nome), resultado.Errors.Single().Metadata["Campo"]);
        repositorioMateria.Verify(r => r.Cadastrar(It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosValidos_AtualizaMateria()
    {
        var (disciplina, materia) = CriarMateria();
        var dto = new EditarMateriaDto(materia.Id, "Geometria", 8, disciplina.Id);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        Materia? atualizada = null;
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.Editar(materia.Id, It.IsAny<Materia>()))
            .Callback<Guid, Materia>((_, entidade) => atualizada = entidade)
            .Returns(true);

        Result resultado = servicoMateria.Editar(dto);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(atualizada);
        Assert.AreEqual("Geometria", atualizada.Nome);
        Assert.AreEqual(8, atualizada.Serie);
        Assert.AreSame(disciplina, atualizada.Disciplina);
    }

    [TestMethod]
    public void Editar_NomeDuplicado_RetornaFalha()
    {
        var (disciplina, materia) = CriarMateria();
        var outra = new Materia("Geometria", 7, disciplina);
        var dto = new EditarMateriaDto(materia.Id, " GEOMETRIA ", 7, disciplina.Id);
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia, outra);

        Result resultado = servicoMateria.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(EditarMateriaDto.Nome), resultado.Errors.Single().Metadata["Campo"]);
        repositorioMateria.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DisciplinaInexistente_RetornaFalha()
    {
        var (_, materia) = CriarMateria();
        Guid disciplinaId = Guid.CreateVersion7();
        var dto = CriarDtoEdicao(materia.Id, disciplinaId);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplinaId)).Returns((Disciplina?)null);

        Result resultado = servicoMateria.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(EditarMateriaDto.DisciplinaId), resultado.Errors.Single().Metadata["Campo"]);
        repositorioMateria.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Materia>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosInvalidos_RetornaFalha()
    {
        var (disciplina, materia) = CriarMateria();
        var dto = new EditarMateriaDto(materia.Id, " ", 7, disciplina.Id);
        var (repositorioMateria, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);

        Result resultado = servicoMateria.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        repositorioMateria.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Materia>()), Times.Never);
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

    [TestMethod]
    public void Excluir_MateriaVinculadaAProva_RetornaFalhaAmigavelAntesDeExcluir()
    {
        var (disciplina, materia) = CriarMateria();
        var questao = new Questao(
            "Quanto é 2 + 2?",
            materia,
            [new Alternativa("4", true), new Alternativa("5", false)]
        );
        var prova = new Prova("Avaliação de Álgebra", disciplina, materia, 7, 1, false, [questao]);
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioProva> repositorioProva = new();
        var servico = new ServicoMateria(
            repositorioMateria.Object,
            repositorioDisciplina.Object,
            repositorioQuestao.Object,
            repositorioProva.Object
        );
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.ConfigurarSelecao(questao);
        repositorioProva.ConfigurarSelecao(prova);

        Result resultado = servico.Excluir(materia.Id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            "Não é possível excluir esta matéria, pois ela está vinculada a uma prova.",
            resultado.Errors.Single().Message
        );
        repositorioMateria.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void Excluir_MateriaSemQuestoes_ExcluiMateria()
    {
        var (_, materia) = CriarMateria();
        var (repositorioMateria, _, repositorioQuestao, servicoMateria) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.ConfigurarSelecao();

        Result resultado = servicoMateria.Excluir(materia.Id);

        Assert.IsTrue(resultado.IsSuccess);
        repositorioMateria.Verify(r => r.Excluir(materia.Id), Times.Once);
    }

    [TestMethod]
    public void Excluir_MateriaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(id)).Returns((Materia?)null);

        Result resultado = servicoMateria.Excluir(id);

        Assert.IsTrue(resultado.IsFailed);
        repositorioMateria.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void SelecionarTodos_MateriasExistentes_RetornaDtosMapeados()
    {
        var (disciplina, materia) = CriarMateria();
        var outra = new Materia("Geometria", 8, disciplina);
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia, outra);

        List<ListarMateriaDto> resultado = servicoMateria.SelecionarTodos();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(materia.Id, resultado[0].Id);
        Assert.AreEqual("Álgebra", resultado[0].Nome);
        Assert.AreEqual(7, resultado[0].Serie);
        Assert.AreEqual("Matemática", resultado[0].NomeDisciplina);
    }

    [TestMethod]
    public void SelecionarPorId_MateriaExistente_RetornaDetalhesMapeados()
    {
        var (disciplina, materia) = CriarMateria();
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);

        Result<DetalhesMateriaDto> resultado = servicoMateria.SelecionarPorId(materia.Id);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(materia.Id, resultado.Value.Id);
        Assert.AreEqual(disciplina.Id, resultado.Value.DisciplinaId);
        Assert.AreEqual(disciplina.Nome, resultado.Value.NomeDisciplina);
    }

    [TestMethod]
    public void SelecionarPorId_MateriaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioMateria, _, _, servicoMateria) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(id)).Returns((Materia?)null);

        Result<DetalhesMateriaDto> resultado = servicoMateria.SelecionarPorId(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void SelecionarDisciplinas_DisciplinasExistentes_RetornaOpcoesMapeadas()
    {
        var primeira = new Disciplina("Matemática");
        var segunda = new Disciplina("Física");
        var (_, repositorioDisciplina, _, servicoMateria) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao(primeira, segunda);

        List<OpcaoDisciplinaMateriaDto> resultado = servicoMateria.SelecionarDisciplinas();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(primeira.Id, resultado[0].Id);
        Assert.AreEqual("Matemática", resultado[0].Nome);
        Assert.AreEqual(segunda.Id, resultado[1].Id);
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
