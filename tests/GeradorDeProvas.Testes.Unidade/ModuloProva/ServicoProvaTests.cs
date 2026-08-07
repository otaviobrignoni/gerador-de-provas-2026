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
    public void Cadastrar_DadosInvalidos_RetornaFalha()
    {
        var (disciplina, materia, _, _) = CriarProva(0);
        var dto = new CadastrarProvaDto(" ", disciplina.Id, materia.Id, 7, 0, false);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Título", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_DisciplinaInexistente_RetornaFalha()
    {
        var (_, materia, _, _) = CriarProva(0);
        Guid disciplinaId = Guid.CreateVersion7();
        var dto = new CadastrarProvaDto("Avaliação", disciplinaId, materia.Id, 7, 1, false);
        var (repositorioProva, repositorioDisciplina, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplinaId)).Returns((Disciplina?)null);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.DisciplinaId), resultado.Errors.Single().Metadata["Campo"]);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_SemMateriaEmProvaRegular_RetornaFalha()
    {
        var (disciplina, _, _, _) = CriarProva(0);
        var dto = new CadastrarProvaDto("Avaliação", disciplina.Id, null, 7, 1, false);
        var (repositorioProva, repositorioDisciplina, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.MateriaId), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("matéria válida", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void Cadastrar_MateriaInexistente_RetornaFalha()
    {
        var (disciplina, _, _, _) = CriarProva(0);
        Guid materiaId = Guid.CreateVersion7();
        var dto = new CadastrarProvaDto("Avaliação", disciplina.Id, materiaId, 7, 1, false);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materiaId)).Returns((Materia?)null);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(CadastrarProvaDto.MateriaId), resultado.Errors.Single().Metadata["Campo"]);
        Assert.Contains("não pertence", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void Cadastrar_SemIds_SorteiaQuestoesAutomaticamente()
    {
        var (disciplina, materia, questoes, _) = CriarProva(3);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        Prova? cadastrada = null;
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioQuestao.ConfigurarSelecao([.. questoes]);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>())).Callback<Prova>(p => cadastrada = p);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(cadastrada);
        Assert.HasCount(2, cadastrada.Questoes);
        Assert.IsTrue(cadastrada.Questoes.All(questoes.Contains));
    }

    [TestMethod]
    public void Cadastrar_QuestoesInsuficientes_RetornaFalha()
    {
        var (disciplina, materia, questoes, _) = CriarProva(1);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);
        repositorioQuestao.ConfigurarSelecao([.. questoes]);

        Result resultado = servicoProva.Cadastrar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("maior que a quantidade disponível", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_ProvaRecuperacao_SelecionaQuestoesDeTodasMateriasElegiveis()
    {
        var disciplina = new Disciplina("Matemática");
        var algebra = new Materia("Álgebra", 7, disciplina);
        var geometria = new Materia("Geometria", 7, disciplina);
        var primeira = new Questao("Quanto é 2 + 2?", algebra, CriarAlternativas());
        var segunda = new Questao("Qual é a área do quadrado?", geometria, CriarAlternativas("L²", "2L"));
        var dto = new CadastrarProvaDto("Recuperação", disciplina.Id, null, 7, 2, true);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        Prova? cadastrada = null;
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.ConfigurarSelecao(algebra, geometria);
        repositorioQuestao.ConfigurarSelecao(primeira, segunda);
        repositorioProva.Setup(r => r.Cadastrar(It.IsAny<Prova>())).Callback<Prova>(p => cadastrada = p);

        Result resultado = servicoProva.Cadastrar(dto, [primeira.Id, segunda.Id]);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.IsNotNull(cadastrada);
        Assert.IsTrue(cadastrada.ProvaRecuperacao);
        Assert.IsNull(cadastrada.Materia);
        CollectionAssert.AreEqual(new[] { primeira.Id, segunda.Id }, cadastrada.Questoes.Select(q => q.Id).ToArray());
    }

    [TestMethod]
    public void Cadastrar_QuantidadeDeIdsDivergente_RetornaFalha()
    {
        var (disciplina, materia, questoes, _) = CriarProva(1);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, _, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(repositorioProva, repositorioDisciplina, repositorioMateria, disciplina, materia);

        Result resultado = servicoProva.Cadastrar(dto, [questoes[0].Id]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("quantidade de questões confirmada", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Cadastrar_IdsDuplicados_RetornaFalha()
    {
        var (disciplina, materia, questoes, _) = CriarProva(1);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, _, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(repositorioProva, repositorioDisciplina, repositorioMateria, disciplina, materia);

        Result resultado = servicoProva.Cadastrar(dto, [questoes[0].Id, questoes[0].Id]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("quantidade de questões confirmada", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void Cadastrar_IdDeQuestaoInexistente_RetornaFalha()
    {
        var (disciplina, materia, _, _) = CriarProva(0);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 1);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(repositorioProva, repositorioDisciplina, repositorioMateria, disciplina, materia);
        repositorioQuestao.ConfigurarSelecao();

        Result resultado = servicoProva.Cadastrar(dto, [Guid.CreateVersion7()]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não pertencem à configuração", resultado.Errors.Single().Message);
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
    public void Duplicar_TituloDuplicado_RetornaFalha()
    {
        var (_, _, _, prova) = CriarProva();
        var dto = new DuplicarProvaDto(prova.Id, " AVALIAÇÃO DE ÁLGEBRA ");
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao(prova);

        Result resultado = servicoProva.Duplicar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(nameof(DuplicarProvaDto.Titulo), resultado.Errors.Single().Metadata["Campo"]);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Duplicar_ProvaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var dto = new DuplicarProvaDto(id, "Nova avaliação");
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao();
        repositorioProva.Setup(r => r.SelecionarPorId(id)).Returns((Prova?)null);

        Result resultado = servicoProva.Duplicar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Duplicar_TituloInvalido_RetornaFalha()
    {
        var (_, _, _, prova) = CriarProva();
        var dto = new DuplicarProvaDto(prova.Id, " ");
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao(prova);
        repositorioProva.Setup(r => r.SelecionarPorId(prova.Id)).Returns(prova);

        Result resultado = servicoProva.Duplicar(dto);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("Título", resultado.Errors.Single().Message);
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Excluir_ProvaExistente_ExcluiProva()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.Setup(r => r.Excluir(id)).Returns(true);

        Result resultado = servicoProva.Excluir(id);

        Assert.IsTrue(resultado.IsSuccess);
        repositorioProva.Verify(r => r.Excluir(id), Times.Once);
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

    [TestMethod]
    public void SelecionarTodos_ProvasExistentes_RetornaDtosMapeados()
    {
        var (_, _, _, prova) = CriarProva();
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.ConfigurarSelecao(prova);

        List<ListarProvaDto> resultado = servicoProva.SelecionarTodos();

        Assert.HasCount(1, resultado);
        Assert.AreEqual(prova.Id, resultado[0].Id);
        Assert.AreEqual(prova.Titulo, resultado[0].Titulo);
        Assert.AreEqual(prova.Disciplina.Nome, resultado[0].NomeDisciplina);
        Assert.AreEqual(prova.Materia!.Nome, resultado[0].NomeMateria);
        Assert.AreEqual(prova.QuantidadeQuestoes, resultado[0].QuantidadeQuestoes);
    }

    [TestMethod]
    public void SelecionarPorId_ProvaExistente_RetornaDetalhesMapeados()
    {
        var (_, _, questoes, prova) = CriarProva(1);
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.Setup(r => r.SelecionarPorId(prova.Id)).Returns(prova);

        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(prova.Id);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.AreEqual(prova.Id, resultado.Value.Id);
        Assert.AreEqual(prova.Disciplina.Id, resultado.Value.DisciplinaId);
        Assert.AreEqual(prova.Materia!.Id, resultado.Value.MateriaId);
        Assert.HasCount(1, resultado.Value.Questoes);
        Assert.AreEqual(questoes[0].Id, resultado.Value.Questoes[0].Id);
        Assert.HasCount(2, resultado.Value.Questoes[0].Alternativas);
    }

    [TestMethod]
    public void SelecionarPorId_ProvaInexistente_RetornaFalha()
    {
        Guid id = Guid.CreateVersion7();
        var (repositorioProva, _, _, _, servicoProva) = CriarServico();
        repositorioProva.Setup(r => r.SelecionarPorId(id)).Returns((Prova?)null);

        Result<DetalhesProvaDto> resultado = servicoProva.SelecionarPorId(id);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não encontrada", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void SortearQuestoes_ConfiguracaoValida_RetornaQuestoesMapeadasSemPersistir()
    {
        var (disciplina, materia, questoes, _) = CriarProva(2);
        var dto = CriarDtoCadastro(disciplina, materia, quantidadeQuestoes: 2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(repositorioProva, repositorioDisciplina, repositorioMateria, disciplina, materia);
        repositorioQuestao.ConfigurarSelecao([.. questoes]);

        Result<List<QuestaoProvaDto>> resultado = servicoProva.SortearQuestoes(dto);

        Assert.IsTrue(resultado.IsSuccess);
        Assert.HasCount(2, resultado.Value);
        CollectionAssert.AreEquivalent(questoes.Select(q => q.Id).ToArray(), resultado.Value.Select(q => q.Id).ToArray());
        Assert.IsTrue(resultado.Value.All(q => q.Alternativas.Count == 2));
        repositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void SelecionarQuestoes_IdsExistentes_RetornaDtosNaOrdemInformada()
    {
        var (_, _, questoes, _) = CriarProva(2);
        var (_, _, _, repositorioQuestao, servicoProva) = CriarServico();
        repositorioQuestao.ConfigurarSelecao([.. questoes]);
        Guid[] ids = [questoes[1].Id, questoes[0].Id];

        Result<List<QuestaoProvaDto>> resultado = servicoProva.SelecionarQuestoes(ids);

        Assert.IsTrue(resultado.IsSuccess);
        CollectionAssert.AreEqual(ids, resultado.Value.Select(q => q.Id).ToArray());
        Assert.AreEqual(questoes[1].Enunciado, resultado.Value[0].Enunciado);
    }

    [TestMethod]
    public void SelecionarQuestoes_IdInexistente_RetornaFalha()
    {
        var (_, _, questoes, _) = CriarProva(1);
        var (_, _, _, repositorioQuestao, servicoProva) = CriarServico();
        repositorioQuestao.ConfigurarSelecao([.. questoes]);

        Result<List<QuestaoProvaDto>> resultado = servicoProva.SelecionarQuestoes([questoes[0].Id, Guid.CreateVersion7()]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não foram encontradas", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void SelecionarQuestoes_IdsDuplicados_RetornaFalha()
    {
        var (_, _, questoes, _) = CriarProva(1);
        var (_, _, _, repositorioQuestao, servicoProva) = CriarServico();
        repositorioQuestao.ConfigurarSelecao([.. questoes]);

        Result<List<QuestaoProvaDto>> resultado = servicoProva.SelecionarQuestoes([questoes[0].Id, questoes[0].Id]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.Contains("não foram encontradas", resultado.Errors.Single().Message);
    }

    [TestMethod]
    public void SelecionarQuestoes_ConfiguracaoEIdsValidos_RetornaDtosNaOrdemConfirmada()
    {
        var (disciplina, materia, questoes, _) = CriarProva(2);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(
            repositorioProva,
            repositorioDisciplina,
            repositorioMateria,
            disciplina,
            materia
        );
        repositorioQuestao.ConfigurarSelecao([.. questoes]);
        List<Guid> idsConfirmados = [questoes[1].Id, questoes[0].Id];
        CadastrarProvaDto dto = CriarDtoCadastro(disciplina, materia);

        Result<List<QuestaoProvaDto>> resultado = servicoProva
            .SelecionarQuestoes(dto, idsConfirmados);

        Assert.IsTrue(resultado.IsSuccess);
        CollectionAssert.AreEqual(
            idsConfirmados,
            resultado.Value.Select(q => q.Id).ToList()
        );
        CollectionAssert.AreEqual(
            new[] { questoes[1].Enunciado, questoes[0].Enunciado },
            resultado.Value.Select(q => q.Enunciado).ToArray()
        );
    }

    [TestMethod]
    public void SelecionarQuestoes_ConfiguracaoAdulterada_RetornaFalha()
    {
        var (disciplina, materia, questoes, _) = CriarProva(1);
        var (repositorioProva, repositorioDisciplina, repositorioMateria, repositorioQuestao, servicoProva) = CriarServico();
        ConfigurarDependenciasDaProva(
            repositorioProva,
            repositorioDisciplina,
            repositorioMateria,
            disciplina,
            materia
        );
        repositorioQuestao.ConfigurarSelecao([.. questoes]);
        CadastrarProvaDto dto = CriarDtoCadastro(
            disciplina,
            materia,
            quantidadeQuestoes: 1
        );

        Result<List<QuestaoProvaDto>> resultado = servicoProva
            .SelecionarQuestoes(dto, [Guid.CreateVersion7()]);

        Assert.IsTrue(resultado.IsFailed);
        Assert.AreEqual(
            nameof(CadastrarProvaDto.QuantidadeQuestoes),
            resultado.Errors.Single().Metadata["Campo"]
        );
        Assert.AreEqual(
            "Uma ou mais questões confirmadas não pertencem à configuração da prova.",
            resultado.Errors.Single().Message
        );
    }

    [TestMethod]
    public void SelecionarDisciplinas_DisciplinasExistentes_RetornaOpcoesMapeadas()
    {
        var primeira = new Disciplina("Matemática");
        var segunda = new Disciplina("Física");
        var (_, repositorioDisciplina, _, _, servicoProva) = CriarServico();
        repositorioDisciplina.ConfigurarSelecao(primeira, segunda);

        List<OpcaoDisciplinaProvaDto> resultado = servicoProva.SelecionarDisciplinas();

        Assert.HasCount(2, resultado);
        Assert.AreEqual(primeira.Id, resultado[0].Id);
        Assert.AreEqual(primeira.Nome, resultado[0].Nome);
    }

    [TestMethod]
    public void SelecionarMaterias_DisciplinaESerie_RetornaSomenteOpcoesElegiveis()
    {
        var disciplina = new Disciplina("Matemática");
        var outraDisciplina = new Disciplina("Física");
        var algebra = new Materia("Álgebra", 7, disciplina);
        var geometria = new Materia("Geometria", 8, disciplina);
        var mecanica = new Materia("Mecânica", 7, outraDisciplina);
        var (_, _, repositorioMateria, _, servicoProva) = CriarServico();
        repositorioMateria.ConfigurarSelecao(algebra, geometria, mecanica);

        List<OpcaoMateriaProvaDto> resultado = servicoProva.SelecionarMaterias(disciplina.Id, 7);

        Assert.HasCount(1, resultado);
        Assert.AreEqual(algebra.Id, resultado[0].Id);
        Assert.AreEqual(algebra.Nome, resultado[0].Nome);
        Assert.AreEqual(7, resultado[0].Serie);
    }

    [TestMethod]
    public void SelecionarMaterias_Disciplina_RetornaOpcoesDeTodasAsSeries()
    {
        var disciplina = new Disciplina("Matemática");
        var algebra = new Materia("Álgebra", 7, disciplina);
        var geometria = new Materia("Geometria", 8, disciplina);
        var (_, _, repositorioMateria, _, servicoProva) = CriarServico();
        repositorioMateria.ConfigurarSelecao(algebra, geometria);

        List<OpcaoMateriaProvaDto> resultado = servicoProva.SelecionarMaterias(disciplina.Id);

        Assert.HasCount(2, resultado);
        CollectionAssert.AreEqual(new[] { algebra.Id, geometria.Id }, resultado.Select(m => m.Id).ToArray());
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

    private static void ConfigurarDependenciasDaProva(
        Mock<IRepositorioProva> repositorioProva,
        Mock<IRepositorioDisciplina> repositorioDisciplina,
        Mock<IRepositorioMateria> repositorioMateria,
        Disciplina disciplina,
        Materia materia)
    {
        repositorioProva.ConfigurarSelecao();
        repositorioDisciplina.Setup(r => r.SelecionarPorId(disciplina.Id)).Returns(disciplina);
        repositorioMateria.Setup(r => r.SelecionarPorId(materia.Id)).Returns(materia);
        repositorioMateria.ConfigurarSelecao(materia);
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
