using FluentResults;
using GeradorDeProvas.Aplicacao.ModuloQuestao;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.Testes.Unidade.Compartilhado;
using Moq;

namespace GeradorDeProvas.Testes.Unidade.ModuloQuestao;

[TestClass]
public sealed class ServicoQuestaoTests
{
    [TestMethod]
    public void Cadastrar_DadosValidos_CadastraQuestaoComAlternativas()
    {
        // Arrange
        var (_, materia, _) = CriarQuestao();
        var dto = CriarDtoCadastro(materia.Id);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        Questao? questaoCadastrada = null;
        repositorioQuestao.Setup(r => r.Cadastrar(It.IsAny<Questao>()))
            .Callback<Questao>(questao => questaoCadastrada = questao);

        // Act
        Result resultado = servicoQuestao.Cadastrar(dto);

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
        var dto = CriarDtoCadastro(materiaId);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materiaId)).Returns((Materia?)null);

        // Act
        Result resultado = servicoQuestao.Cadastrar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual("MateriaId", resultado.Errors.Single().Metadata["Campo"]);
        repositorioQuestao.Verify(r => r.Cadastrar(It.IsAny<Questao>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_DadosInvalidos_RetornaFalha()
    {
        var (_, materia, _) = CriarQuestao();
        var dto = new CadastrarQuestaoDto(" ", materia.Id, CriarAlternativasDto());
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);

        Result resultado = servicoQuestao.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Enunciado", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Cadastrar(It.IsAny<Questao>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosValidos_AtualizaQuestaoComAlternativas()
    {
        var (_, materia, questao) = CriarQuestao();
        var alternativas = CriarAlternativasDto(10, false);
        var dto = new EditarQuestaoDto(questao.Id, "Quanto é 5 + 5?", materia.Id, alternativas);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        Questao? atualizada = null;
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.Setup(r => r.Editar(questao.Id, It.IsAny<Questao>()))
            .Callback<Guid, Questao>((_, entidade) => atualizada = entidade)
            .Returns(true);

        Result resultado = servicoQuestao.Editar(dto);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(atualizada);
        Assert.AreEqual(dto.Enunciado, atualizada.Enunciado);
        Assert.AreSame(materia, atualizada.Materia);
        Assert.HasCount(2, atualizada.Alternativas);
        Assert.AreEqual("10", atualizada.Alternativas[0].Texto);
        Assert.IsFalse(atualizada.Alternativas[0].Correta);
    }

    [TestMethod]
    public void Editar_MateriaInexistente_RetornaFalha()
    {
        Guid materiaId = Guid.CreateVersion7();
        var dto = CriarDtoEdicao(Guid.CreateVersion7(), materiaId);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materiaId)).Returns((Materia?)null);

        Result resultado = servicoQuestao.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(EditarQuestaoDto.MateriaId), resultado.Errors.Single().Metadata["Campo"]);
        repositorioQuestao.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Questao>()), Times.Never);
    }

    [TestMethod]
    public void Editar_DadosInvalidos_RetornaFalha()
    {
        var (_, materia, questao) = CriarQuestao();
        var dto = new EditarQuestaoDto(questao.Id, questao.Enunciado, materia.Id, [new CadastrarAlternativaDto("4", true)]);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);

        Result resultado = servicoQuestao.Editar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("mínimo duas", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Editar(It.IsAny<Guid>(), It.IsAny<Questao>()), Times.Never);
    }

    [TestMethod]
    public void Editar_QuestaoInexistente_RetornaFalha()
    {
        // Arrange
        var (_, materia, _) = CriarQuestao();
        Guid questaoId = Guid.CreateVersion7();
        var dto = CriarDtoEdicao(questaoId, materia.Id);
        var (repositorioQuestao, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioQuestao.Setup(r => r.Editar(questaoId, It.IsAny<Questao>())).Returns(false);

        // Act
        Result resultado = servicoQuestao.Editar(dto);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Questão não encontrada", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Editar(questaoId, It.IsAny<Questao>()), Times.Once);
    }

    [TestMethod]
    public void Excluir_QuestaoVinculadaAProva_RetornaFalha()
    {
        // Arrange
        var (disciplina, materia, questao) = CriarQuestao();
        var prova = new Prova("Avaliação", disciplina, materia, 7, 1, false, [questao]);
        var (repositorioQuestao, _, repositorioProva, servico) = CriarServico(criarRepositorioProva: true);
        repositorioQuestao.Setup(r => r.SelecionarPorId(questao.Id)).Returns(questao);
        repositorioProva!.ConfigurarSelecao(prova);

        // Act
        Result resultado = servico.Excluir(questao.Id);

        // Assert
        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("vinculada a uma prova", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void Excluir_QuestaoSemProva_ExcluiQuestao()
    {
        var (_, _, questao) = CriarQuestao();
        var (repositorioQuestao, _, _, servicoQuestao) = CriarServico();
        repositorioQuestao.Setup(r => r.SelecionarPorId(questao.Id)).Returns(questao);

        Result resultado = servicoQuestao.Excluir(questao.Id);

        Assert.IsTrue(resultado.IsSuccess);
        repositorioQuestao.Verify(r => r.Excluir(questao.Id), Times.Once);
    }

    [TestMethod]
    public void Excluir_QuestaoInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioQuestao, _, _, servicoQuestao) = CriarServico();
        repositorioQuestao.Setup(r => r.SelecionarPorId(id)).Returns((Questao?)null);

        Result resultado = servicoQuestao.Excluir(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
        repositorioQuestao.Verify(r => r.Excluir(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public void SelecionarTodos_QuestoesExistentes_RetornaDtosMapeados()
    {
        var (_, materia, primeira) = CriarQuestao();
        var segunda = new Questao("Quanto é 3 + 3?", materia, CriarAlternativas(6));
        var (repositorioQuestao, _, _, servicoQuestao) = CriarServico();
        repositorioQuestao.ConfigurarSelecao(primeira, segunda);

        List<ListarQuestaoDto> resultado = servicoQuestao.SelecionarTodos();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(primeira.Id, resultado[0].Id);
        Assert.AreEqual(primeira.Enunciado, resultado[0].Enunciado);
        Assert.AreEqual(materia.Nome, resultado[0].NomeMateria);
        Assert.AreEqual("4", resultado[0].RespostaCorreta);
    }

    [TestMethod]
    public void SelecionarPorId_QuestaoExistente_RetornaDetalhesMapeados()
    {
        var (_, materia, questao) = CriarQuestao();
        var (repositorioQuestao, _, _, servicoQuestao) = CriarServico();
        repositorioQuestao.Setup(r => r.SelecionarPorId(questao.Id)).Returns(questao);

        Result<DetalhesQuestaoDto> resultado = servicoQuestao.SelecionarPorId(questao.Id);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(questao.Id, resultado.Value.Id);
        Assert.AreEqual(materia.Id, resultado.Value.MateriaId);
        Assert.AreEqual(materia.Nome, resultado.Value.NomeMateria);
        Assert.HasCount(2, resultado.Value.Alternativas);
        Assert.AreEqual(questao.Alternativas[0].Id, resultado.Value.Alternativas[0].Id);
    }

    [TestMethod]
    public void SelecionarPorId_QuestaoInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioQuestao, _, _, servicoQuestao) = CriarServico();
        repositorioQuestao.Setup(r => r.SelecionarPorId(id)).Returns((Questao?)null);

        Result<DetalhesQuestaoDto> resultado = servicoQuestao.SelecionarPorId(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void SelecionarMaterias_MateriasExistentes_RetornaOpcoesMapeadas()
    {
        var (disciplina, materia, _) = CriarQuestao();
        var outra = new Materia("Geometria", 7, disciplina);
        var (_, repositorioMateria, _, servicoQuestao) = CriarServico();
        repositorioMateria.ConfigurarSelecao(materia, outra);

        List<OpcaoMateriaQuestaoDto> resultado = servicoQuestao.SelecionarMaterias();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(materia.Id, resultado[0].Id);
        Assert.AreEqual("Álgebra", resultado[0].Nome);
        Assert.AreEqual(outra.Id, resultado[1].Id);
    }

    private static (Disciplina disciplina, Materia materia, Questao questao) CriarQuestao()
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 7, disciplina);
        var questao = new Questao("Quanto é 2 + 2?", materia, CriarAlternativas());

        return (disciplina, materia, questao);
    }

    private static List<Alternativa> CriarAlternativas(int primeiroValor = 4, bool primeiraCorreta = true)
    {
        return [new Alternativa($"{primeiroValor}", primeiraCorreta), new Alternativa($"{primeiroValor + 1}", !primeiraCorreta)];
    }

    private static List<CadastrarAlternativaDto> CriarAlternativasDto(int primeiroValor = 4, bool primeiraCorreta = true)
    {
        return [new CadastrarAlternativaDto($"{primeiroValor}", primeiraCorreta), new CadastrarAlternativaDto($"{primeiroValor + 1}", !primeiraCorreta)];
    }

    private static CadastrarQuestaoDto CriarDtoCadastro(Guid materiaId)
    {
        return new CadastrarQuestaoDto("Quanto é 2 + 2?", materiaId, CriarAlternativasDto());
    }

    private static EditarQuestaoDto CriarDtoEdicao(Guid id, Guid materiaId)
    {
        return new EditarQuestaoDto(id, "Quanto é 2 + 2?", materiaId, CriarAlternativasDto());
    }

    private static (Mock<IRepositorioQuestao> repositorioQuestao, Mock<IRepositorioMateria> repositorioMateria, Mock<IRepositorioProva>? repositorioProva, ServicoQuestao servicoQuestao) CriarServico(bool criarRepositorioProva = false)
    {
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioProva>? repositorioProva = criarRepositorioProva ? new() : null;

        ServicoQuestao servicoQuestao = new(repositorioQuestao.Object, repositorioMateria.Object, repositorioProva?.Object);

        return (repositorioQuestao, repositorioMateria, repositorioProva, servicoQuestao);
    }
}
