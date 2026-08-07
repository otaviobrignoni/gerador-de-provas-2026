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
