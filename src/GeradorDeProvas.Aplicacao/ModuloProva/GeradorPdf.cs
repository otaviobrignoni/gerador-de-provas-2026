using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

namespace GeradorDeProvas.Aplicacao.ModuloProva;

public static class GeradorPdf
{
    private const string FonteJetBrainsMonoRegular = "GeradorDeProvas.Aplicacao.Fonts.JetBrainsMono-Regular.ttf";
    private const string FonteJetBrainsMonoBold = "GeradorDeProvas.Aplicacao.Fonts.JetBrainsMono-Bold.ttf";

    private static readonly Lazy<bool> fontesInicializadas = new(RegistrarFontes, true);

    public static byte[] GerarPdf(this DetalhesProvaDto prova, bool incluirGabarito)
    {
        return CriarDocumento(prova, incluirGabarito).GeneratePdf();
    }

    public static void GerarPdfEMostrar(this DetalhesProvaDto prova, bool incluirGabarito)
    {
        CriarDocumento(prova, incluirGabarito).GeneratePdfAndShow();
    }

    private static Document CriarDocumento(DetalhesProvaDto prova, bool incluirGabarito)
    {
        _ = fontesInicializadas.Value;

        return Document.Create(document =>
        {
            document.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.PageColor(Colors.White);
                pagina.DefaultTextStyle(style => style
                    .FontFamily("JetBrains Mono")
                    .FontSize(11));

                pagina.Header().Column(header =>
                {
                    header.Item()
                        .Text(prova.Titulo)
                        .Bold()
                        .FontSize(18)
                        .FontColor(Colors.Blue.Darken2);
                    header.Item().PaddingTop(4).Text(texto =>
                    {
                        texto.Span($"Disciplina: {prova.NomeDisciplina}, ");
                        texto.Span(prova.ProvaRecuperacao ? "Prova de recuperação, " : $"Matéria: {prova.NomeMateria}, ");
                        texto.Span($"Série: {prova.Serie}");
                    });
                    header.Item().PaddingTop(8)
                        .LineHorizontal(1)
                        .LineColor(Colors.Grey.Lighten1);
                });

                pagina.Content().PaddingVertical(15).Column(conteudo =>
                {
                    conteudo.Spacing(12);
                    for (int i = 0; i < prova.Questoes.Count; i++)
                    {
                        QuestaoProvaDto questaoDto = prova.Questoes[i];
                        conteudo.Item().PreventPageBreak().Column(questao =>
                        {
                            questao.Spacing(5);
                            questao.Item().Text(texto =>
                            {
                                texto.Span($"{i + 1}. ").Bold();
                                texto.Span(questaoDto.Enunciado);
                            });

                            foreach (var a in questaoDto.Alternativas)
                                questao.Item().PaddingLeft(15)
                                    .Text($"{(incluirGabarito && a.Correta ? "[X]" : "[ ]")} {a.Texto}");
                        });
                    }
                });

                pagina.Footer().AlignCenter().Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        });
    }

    private static bool RegistrarFontes()
    {
        FontManager.RegisterFontFromEmbeddedResource(FonteJetBrainsMonoRegular);
        FontManager.RegisterFontFromEmbeddedResource(FonteJetBrainsMonoBold);

        return true;
    }
}
