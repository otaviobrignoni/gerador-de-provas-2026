using GeradorDeProvas.Testes.E2E.Compartilhado;
using GeradorDeProvas.Testes.E2E.ModuloDisciplina;
using GeradorDeProvas.Testes.E2E.ModuloMateria;
using GeradorDeProvas.Testes.E2E.ModuloQuestao;
using Microsoft.Playwright;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

[TestClass]
public sealed class ProvaE2ETests : E2ETestsBase
{
    [TestMethod]
    public async Task DeveExibir_ListagemVazia_ParaUsuario_SemProvas()
    {
        // Arrange
        await RegistrarEEntrarAsync("prova.listagem@teste.local", "Senha123!");

        ProvaListarPage listarPage = new(Page, Url);

        // Act
        await listarPage.IrParaAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.Titulo).ToBeVisibleAsync();
        await Expect(listarPage.GerarNova).ToBeVisibleAsync();
        await Expect(listarPage.EstadoVazio).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveGerar_Prova_ComDadosValidos()
    {
        // Arrange
        ProvaListarPage listarPage = await GerarProvaAsync("prova.geracao@teste.local", "Prova de Matemática");

        // Act
        await Expect(listarPage.TituloDaProva("Prova de Matemática")).ToBeVisibleAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.EstadoVazio).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task DeveManter_DoisFluxosDeGeracao_EmAbasDiferentes()
    {
        await PrepararCatalogoAsync("prova.duas.abas@teste.local");
        IPage segundaAba = await Context.NewPageAsync();

        try
        {
            ProvaFormPage formularioPrimeiraAba = new(Page, Url);
            ProvaFormPage formularioSegundaAba = new(segundaAba, Url);
            ProvaSelecionarQuestoesPage selecaoPrimeiraAba = new(Page, Url);
            ProvaSelecionarQuestoesPage selecaoSegundaAba = new(segundaAba, Url);
            ProvaConfirmarPage confirmacaoPrimeiraAba = new(Page, Url);
            ProvaConfirmarPage confirmacaoSegundaAba = new(segundaAba, Url);
            ProvaListarPage listagemSegundaAba = new(segundaAba, Url);

            await formularioPrimeiraAba.IrParaAsync();
            await formularioPrimeiraAba.PreencherAsync("Avaliação da aba A", "Matemática", 7);
            await formularioPrimeiraAba.ContinuarAsync();

            await formularioSegundaAba.IrParaAsync();
            await formularioSegundaAba.PreencherAsync("Avaliação da aba B", "Matemática", 7);
            await formularioSegundaAba.ContinuarAsync();

            await selecaoPrimeiraAba.PreencherAsync("Álgebra", 1);
            await selecaoPrimeiraAba.SortearAsync();
            await Expect(confirmacaoPrimeiraAba.TituloDaProva("Avaliação da aba A")).ToBeVisibleAsync();

            await selecaoSegundaAba.PreencherAsync("Álgebra", 1);
            await selecaoSegundaAba.SortearAsync();
            await Expect(confirmacaoSegundaAba.TituloDaProva("Avaliação da aba B")).ToBeVisibleAsync();

            await confirmacaoPrimeiraAba.ConfirmarAsync();
            await confirmacaoSegundaAba.ConfirmarAsync();

            await Expect(listagemSegundaAba.TituloDaProva("Avaliação da aba A")).ToBeVisibleAsync();
            await Expect(listagemSegundaAba.TituloDaProva("Avaliação da aba B")).ToBeVisibleAsync();
        }
        finally
        {
            await segundaAba.CloseAsync();
        }
    }

    [TestMethod]
    public async Task DeveBaixar_PdfE_Gabarito_DeUmaProvaGerada()
    {
        // Arrange
        const string titulo = "Prova de Matemática";

        ProvaListarPage listarPage = await GerarProvaAsync("prova.pdf@teste.local", titulo);

        // Act
        IDownload pdf = await listarPage.BaixarPdfAsync(titulo);
        IDownload gabarito = await listarPage.BaixarGabaritoAsync(titulo);

        // Assert
        Assert.IsNull(await pdf.FailureAsync());
        Assert.IsTrue(pdf.SuggestedFilename.StartsWith("prova-prova-de-matematica-", StringComparison.Ordinal));
        Assert.IsTrue(pdf.SuggestedFilename.EndsWith(".pdf", StringComparison.Ordinal));
        Assert.IsNull(await gabarito.FailureAsync());
        Assert.IsTrue(gabarito.SuggestedFilename.StartsWith("gabarito-prova-de-matematica-", StringComparison.Ordinal));
        Assert.IsTrue(gabarito.SuggestedFilename.EndsWith(".pdf", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DeveGerar_ProvaDeRecuperacao_ComQuestoes_DeTodasAsMaterias()
    {
        // Arrange
        await RegistrarEEntrarAsync("prova.recuperacao@teste.local", "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastrarMateriaAsync("Álgebra", "Matemática", 7);
        await CadastrarQuestaoAsync("Quanto é 2 + 2?", ["4", "5"], "Álgebra");
        await CadastrarMateriaAsync("Geometria", "Matemática", 7);
        await CadastrarQuestaoAsync("Quantos lados tem um triângulo?", ["3", "4"], "Geometria");

        ProvaFormPage formPage = new(Page, Url);
        ProvaSelecionarQuestoesPage selecionarQuestoesPage = new(Page, Url);
        ProvaConfirmarPage confirmarPage = new(Page, Url);
        ProvaListarPage listarPage = new(Page, Url);

        await formPage.IrParaAsync();
        await formPage.PreencherAsync("Recuperação de Matemática", "Matemática", 7);
        await formPage.MarcarComoRecuperacaoAsync();
        await formPage.ContinuarAsync();

        // Act
        await Expect(selecionarQuestoesPage.AvisoRecuperacao).ToBeVisibleAsync();
        await selecionarQuestoesPage.PreencherRecuperacaoAsync(2);
        await selecionarQuestoesPage.SortearAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(confirmarPage.UrlComFluxo);
        await Expect(confirmarPage.QuestaoSorteada("Quanto é 2 + 2?")).ToBeVisibleAsync();
        await Expect(confirmarPage.QuestaoSorteada("Quantos lados tem um triângulo?")).ToBeVisibleAsync();

        await confirmarPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.TituloDaProva("Recuperação de Matemática")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Deve_ExibirDetalhes_DuplicarEExcluir_ProvaGerada()
    {
        // Arrange
        const string titulo = "Prova para ciclo de vida";
        const string tituloCopia = "Prova para ciclo de vida - Cópia";
        ProvaListarPage listarPage = await GerarProvaAsync("prova.ciclo@teste.local", titulo);
        ProvaDetalhesPage detalhesPage = new(Page, Url);
        ProvaDuplicarPage duplicarPage = new(Page, Url);
        ProvaExcluirPage excluirPage = new(Page, Url);

        // Act e Assert - detalhes
        await listarPage.VerDetalhesAsync(titulo);
        await Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(detalhesPage.UrlBase)}/.*"));
        await Expect(detalhesPage.TituloDaProva(titulo)).ToBeVisibleAsync();
        await Expect(detalhesPage.Questao("Quanto é 2 + 2?")).ToBeVisibleAsync();
        await detalhesPage.VoltarAsync();

        // Act e Assert - duplicação
        await listarPage.DuplicarAsync(titulo);
        await Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(duplicarPage.UrlBase)}/.*"));
        await Expect(duplicarPage.NovoTitulo).ToHaveValueAsync(tituloCopia);
        await duplicarPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.TituloDaProva(tituloCopia)).ToBeVisibleAsync();

        // Act e Assert - exclusão
        await listarPage.ExcluirAsync(tituloCopia);
        await Expect(Page).ToHaveURLAsync(new Regex($"{Regex.Escape(excluirPage.UrlBase)}/.*"));
        await Expect(excluirPage.MensagemConfirmacao).ToBeVisibleAsync();
        await excluirPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.TituloDaProva(tituloCopia)).Not.ToBeVisibleAsync();
        await Expect(listarPage.TituloDaProva(titulo)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task NaoDeveExcluir_QuestaoVinculada_AProva()
    {
        // Arrange
        const string enunciado = "Quanto é 2 + 2?";
        const string mensagemVinculo = "Não é possível excluir esta questão, pois ela está vinculada a uma prova.";
        await GerarProvaAsync("prova.questao.vinculada@teste.local", "Prova com questão vinculada");

        QuestaoListarPage listarPage = new(Page, Url);
        QuestaoExcluirPage excluirPage = new(Page);
        await listarPage.IrParaAsync();
        await listarPage.ExcluirAsync(enunciado);

        // Act
        await excluirPage.ConfirmarAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(listarPage.Url);
        await Expect(listarPage.MensagemErro(mensagemVinculo)).ToBeVisibleAsync();
        await Expect(listarPage.EnunciadoDaQuestao(enunciado)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task NaoDeveSortear_Prova_ComQuantidadeDeQuestoesIndisponivel()
    {
        // Arrange
        const string mensagemErro = "A quantidade de questões informada é maior que a quantidade disponível.";
        await PrepararCatalogoAsync("prova.questoes.insuficientes@teste.local");

        ProvaFormPage formPage = new(Page, Url);
        ProvaSelecionarQuestoesPage selecionarQuestoesPage = new(Page, Url);
        await formPage.IrParaAsync();
        await formPage.PreencherAsync("Prova sem questões suficientes", "Matemática", 7);
        await formPage.ContinuarAsync();
        await selecionarQuestoesPage.PreencherAsync("Álgebra", 2);

        // Act
        await selecionarQuestoesPage.SortearAsync();

        // Assert
        await Expect(Page).ToHaveURLAsync(selecionarQuestoesPage.Url);
        await Expect(selecionarQuestoesPage.MensagemErro(mensagemErro)).ToBeVisibleAsync();
        await Expect(selecionarQuestoesPage.QuantidadeQuestoes).ToHaveValueAsync("2");
    }

    private async Task<ProvaListarPage> GerarProvaAsync(string email, string titulo)
    {
        await PrepararCatalogoAsync(email);

        ProvaFormPage formPage = new(Page, Url);
        ProvaSelecionarQuestoesPage selecionarQuestoesPage = new(Page, Url);
        ProvaConfirmarPage confirmarPage = new(Page, Url);
        ProvaListarPage listarPage = new(Page, Url);

        await formPage.IrParaAsync();
        await formPage.PreencherAsync(titulo, "Matemática", 7);
        await formPage.ContinuarAsync();

        await Expect(Page).ToHaveURLAsync(selecionarQuestoesPage.UrlComFluxo);

        await selecionarQuestoesPage.PreencherAsync("Álgebra", 1);
        await selecionarQuestoesPage.SortearAsync();

        await Expect(Page).ToHaveURLAsync(confirmarPage.UrlComFluxo);
        await Expect(confirmarPage.TituloDaProva(titulo)).ToBeVisibleAsync();
        await Expect(confirmarPage.QuestaoSorteada("Quanto é 2 + 2?")).ToBeVisibleAsync();

        await confirmarPage.ConfirmarAsync();
        await Expect(Page).ToHaveURLAsync(listarPage.Url);

        return listarPage;
    }

    private async Task PrepararCatalogoAsync(string email)
    {
        await RegistrarEEntrarAsync(email, "Senha123!");
        await CadastrarDisciplinaAsync("Matemática");
        await CadastrarMateriaAsync("Álgebra", "Matemática", 7);
        await CadastrarQuestaoAsync("Quanto é 2 + 2?", ["4", "5"]);
    }

    private async Task CadastrarQuestaoAsync(string enunciado, string[] alternativas, string materia = "Álgebra")
    {
        QuestaoFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync(enunciado, materia, alternativas, indiceCorreta: 0);
        await formPage.ConfirmarAsync();

        QuestaoListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }

    private async Task CadastrarMateriaAsync(string nome, string disciplina, int serie)
    {
        MateriaFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherAsync(nome, disciplina, serie);
        await formPage.ConfirmarAsync();

        MateriaListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }

    private async Task CadastrarDisciplinaAsync(string nome)
    {
        DisciplinaFormPage formPage = new(Page, Url);

        await formPage.IrParaCadastroAsync();
        await formPage.PreencherNomeAsync(nome);
        await formPage.ConfirmarAsync();

        DisciplinaListarPage listarPage = new(Page, Url);

        await Expect(Page).ToHaveURLAsync(listarPage.Url);
    }
}
