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
