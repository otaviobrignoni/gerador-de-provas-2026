using GeradorDeProvas.Aplicacao.ModuloProva;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace GeradorDeProvas.Testes.Unidade.ModuloProva;

[TestClass]
public sealed class GeradorPdfTests
{
    [ClassInitialize]
    public static void ConfigurarQuestPdf(TestContext _)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseEnvironmentFonts = false;
    }

    [TestMethod]
    public void Gerar_ProvaNormal_ContemDadosETextoAcentuado()
    {
        // Arrange
        var prova = CriarProva();

        // Act
        byte[] pdf = prova.Gerar(false);

        (int quantidadePaginas, string texto) = LerDocumento(pdf);

        // Assert
        Assert.AreEqual(1, quantidadePaginas);
        Assert.Contains("Avaliação de Matemática", texto);
        Assert.Contains("Disciplina: Matemática", texto);
        Assert.Contains("Matéria: Álgebra", texto);
        Assert.Contains("Série: 7", texto);
        Assert.Contains("Questão 1: comprimento normal", texto);
        Assert.Contains("Alternativa correta com acentuação", texto);
    }

    [TestMethod]
    public void Gerar_Gabarito_MarcaApenasAlternativaCorreta()
    {
        // Arrange
        var prova = CriarProva();

        // Act
        byte[] pdfProva = prova.Gerar(false);
        byte[] pdfGabarito = prova.Gerar(true);

        string textoProva = LerTexto(pdfProva);
        string textoGabarito = LerTexto(pdfGabarito);

        // Assert
        Assert.Contains("[ ] Alternativa correta com acentuação", textoProva);
        Assert.Contains("[ ] Alternativa incorreta", textoProva);
        Assert.Contains("[X] Alternativa correta com acentuação", textoGabarito);
        Assert.Contains("[ ] Alternativa incorreta", textoGabarito);
    }

    [TestMethod]
    public void Gerar_MuitasQuestoes_CriaMultiplasPaginasEConservaTexto()
    {
        // Arrange
        var prova = CriarProva(quantidadeQuestoes: 60, true);

        // Act
        byte[] pdf = prova.Gerar(true);

        (int quantidadePaginas, string texto) = LerDocumento(pdf);

        // Assert
        Assert.IsGreaterThan(1, quantidadePaginas);
        Assert.Contains("Questão 1:", texto);
        Assert.Contains("Questão 60:", texto);
        Assert.Contains($"l{new string('o', 50)}ngo", texto);
    }

    [TestMethod]
    public void Gerar_ProvaRecuperacaoSemMateria_ExibeCabecalhoDeRecuperacao()
    {
        // Arrange
        DetalhesProvaDto prova = CriarProva() with
        {
            MateriaId = null,
            NomeMateria = null,
            ProvaRecuperacao = true
        };

        // Act
        string texto = LerTexto(prova.Gerar(false));

        // Assert
        Assert.Contains("Disciplina: Matemática, Prova de recuperação, Série: 7", texto);
        Assert.DoesNotContain("Matéria:", texto);
    }

    [TestMethod]
    public void Gerar_ProvaVazia_GeraDocumentoComCabecalhoSemQuestoes()
    {
        // Arrange
        DetalhesProvaDto prova = CriarProva(quantidadeQuestoes: 0);

        // Act
        byte[] pdf = prova.Gerar(false);
        (int quantidadePaginas, string texto) = LerDocumento(pdf);

        // Assert
        Assert.AreEqual(1, quantidadePaginas);
        Assert.Contains("Avaliação de Matemática", texto);
        Assert.DoesNotContain("1. Questão", texto);
    }

    [TestMethod]
    public void Gerar_MultiplasQuestoesEAlternativas_PreservaOrdemRecebida()
    {
        // Arrange
        DetalhesProvaDto prova = CriarProva() with
        {
            QuantidadeQuestoes = 2,
            Questoes =
            [
                new QuestaoProvaDto(Guid.CreateVersion7(), "Enunciado primeiro", [
                    new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa primeira A", false),
                    new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa primeira B", true)
                ]),
                new QuestaoProvaDto(Guid.CreateVersion7(), "Enunciado segundo", [
                    new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa segunda A", true),
                    new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa segunda B", false)
                ])
            ]
        };

        // Act
        string texto = LerTexto(prova.Gerar(true));

        // Assert
        int primeiraQuestao = texto.IndexOf("1. Enunciado primeiro", StringComparison.Ordinal);
        int primeiraAlternativaA = texto.IndexOf("[ ] Alternativa primeira A", StringComparison.Ordinal);
        int primeiraAlternativaB = texto.IndexOf("[X] Alternativa primeira B", StringComparison.Ordinal);
        int segundaQuestao = texto.IndexOf("2. Enunciado segundo", StringComparison.Ordinal);
        int segundaAlternativaA = texto.IndexOf("[X] Alternativa segunda A", StringComparison.Ordinal);
        int segundaAlternativaB = texto.IndexOf("[ ] Alternativa segunda B", StringComparison.Ordinal);

        Assert.IsTrue(
            primeiraQuestao < primeiraAlternativaA
            && primeiraAlternativaA < primeiraAlternativaB
            && primeiraAlternativaB < segundaQuestao
            && segundaQuestao < segundaAlternativaA
            && segundaAlternativaA < segundaAlternativaB
        );
    }

    [TestMethod]
    public void Gerar_TextosNosLimitesMaximos_GeraDocumentoEConservaExtremos()
    {
        // Arrange
        string titulo = $"TITULO-{new string('T', 85)}-FIM-TIT";
        string disciplina = $"DISCIPLINA-{new string('D', 74)}-FIM-DISCIPLINA";
        string materia = $"MATERIA-{new string('M', 80)}-FIM-MATERIA";
        string enunciado = $"ENUNCIADO-{new string('E', 1976)}-FIM-ENUNCIADO";
        string alternativa = $"ALTERNATIVA-{new string('A', 972)}-FIM-ALTERNATIVA";
        Assert.AreEqual(100, titulo.Length);
        Assert.AreEqual(100, disciplina.Length);
        Assert.AreEqual(100, materia.Length);
        Assert.AreEqual(2000, enunciado.Length);
        Assert.AreEqual(1000, alternativa.Length);

        DetalhesProvaDto prova = CriarProva() with
        {
            Titulo = titulo,
            NomeDisciplina = disciplina,
            NomeMateria = materia,
            Questoes =
            [
                new QuestaoProvaDto(Guid.CreateVersion7(), enunciado, [
                    new AlternativaProvaDto(Guid.CreateVersion7(), alternativa, true)
                ])
            ]
        };

        // Act
        string texto = LerTexto(prova.Gerar(true));
        string textoSemQuebras = texto.ReplaceLineEndings(string.Empty);

        // Assert
        Assert.Contains("TITULO-", textoSemQuebras);
        Assert.Contains("-FIM-TIT", textoSemQuebras);
        Assert.Contains("DISCIPLINA-", textoSemQuebras);
        Assert.Contains("-FIM-DISCIPLINA", textoSemQuebras);
        Assert.Contains("MATERIA-", textoSemQuebras);
        Assert.Contains("-FIM-MATERIA", textoSemQuebras);
        Assert.Contains("ENUNCIADO-", textoSemQuebras);
        Assert.Contains("-FIM-ENUNCIADO", textoSemQuebras);
        Assert.Contains("ALTERNATIVA-", textoSemQuebras);
        Assert.Contains("-FIM-ALTERNATIVA", textoSemQuebras);
    }

    private static (int, string) LerDocumento(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);

        string texto = string.Join(Environment.NewLine, documento.GetPages().Select(pagina => ContentOrderTextExtractor.GetText(pagina, true)));

        return (documento.NumberOfPages, texto);
    }

    private static string LerTexto(byte[] pdf) => LerDocumento(pdf).Item2;

    private static DetalhesProvaDto CriarProva(int quantidadeQuestoes = 1, bool textoLongo = false)
    {
        string normal = "comprimento normal";
        string longo = $"comprimento l{new string('o', 50)}ngo";
        string enunciado = textoLongo ? longo : normal;
        List<QuestaoProvaDto> questoes = [.. Enumerable.Range(1, quantidadeQuestoes).Select(indice => new QuestaoProvaDto(
            Guid.CreateVersion7(),
            $"Questão {indice}: {enunciado}",
            [
                new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa correta com acentuação", true),
                new AlternativaProvaDto(Guid.CreateVersion7(), "Alternativa incorreta", false)
            ]
        ))];

        return new DetalhesProvaDto(
            Guid.CreateVersion7(),
            "Avaliação de Matemática",
            Guid.CreateVersion7(),
            "Matemática",
            Guid.CreateVersion7(),
            "Álgebra",
            7,
            quantidadeQuestoes,
            false,
            questoes
        );
    }
}
