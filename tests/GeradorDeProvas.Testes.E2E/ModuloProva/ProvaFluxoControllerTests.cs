using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using AutoMapper;
using GeradorDeProvas.Aplicacao.ModuloProva;
using GeradorDeProvas.Dominio.Compartilhado;
using GeradorDeProvas.Dominio.ModuloDisciplina;
using GeradorDeProvas.Dominio.ModuloMateria;
using GeradorDeProvas.Dominio.ModuloProva;
using GeradorDeProvas.Dominio.ModuloQuestao;
using GeradorDeProvas.WebApp.ModuloProva;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;

namespace GeradorDeProvas.Testes.E2E.ModuloProva;

[TestClass]
[TestCategory("Controller")]
public sealed class ProvaFluxoControllerTests
{
    private const string MensagemFluxoAusente = "O fluxo de geração da prova não foi encontrado ou expirou.";
    private const string MensagemEstadoInvalido = "O estado da geração da prova é inválido. Revise as opções e sorteie novamente.";

    [TestMethod]
    public void Confirmar_SemTempData_RedirecionaAoCadastroComMensagem()
    {
        Cenario cenario = CriarCenario();

        ActionResult resultado = cenario.Controller.Confirmar(Guid.CreateVersion7());

        AssertRedirecionamento(resultado, nameof(ProvaController.Cadastrar));
        Assert.AreEqual(MensagemFluxoAusente, cenario.Controller.TempData["MensagemErro"]);
    }

    [TestMethod]
    public void SelecionarQuestoes_ConfiguracaoComJsonInvalido_RedirecionaAoCadastroELimpaEstado()
    {
        Cenario cenario = CriarCenario();
        Guid fluxoId = Guid.CreateVersion7();
        string key = ConfiguracaoKey(fluxoId);
        cenario.Controller.TempData[key] = "{ json inválido";

        ActionResult resultado = cenario.Controller.SelecionarQuestoes(fluxoId);

        AssertRedirecionamento(resultado, nameof(ProvaController.Cadastrar));
        Assert.AreEqual(MensagemEstadoInvalido, cenario.Controller.TempData["MensagemErro"]);
        Assert.IsFalse(cenario.Controller.TempData.ContainsKey(key));
    }

    [TestMethod]
    public void SelecionarQuestoes_DisciplinaRemovidaEntreEtapas_RedirecionaAoCadastroComMensagem()
    {
        Cenario cenario = CriarCenario();
        Guid fluxoId = Guid.CreateVersion7();
        cenario.Controller.TempData[ConfiguracaoKey(fluxoId)] = SerializarConfiguracao(cenario.Disciplina);
        cenario.Disciplinas.Clear();

        ActionResult resultado = cenario.Controller.SelecionarQuestoes(fluxoId);

        AssertRedirecionamento(resultado, nameof(ProvaController.Cadastrar));
        Assert.AreEqual("A disciplina selecionada não foi encontrada.", cenario.Controller.TempData["MensagemErro"]);
    }

    [TestMethod]
    public void Confirmar_MateriaRemovidaDepoisDoSorteio_RetornaASelecaoComMensagem()
    {
        Cenario cenario = CriarCenario();
        Guid fluxoId = PrepararEstado(cenario);
        cenario.Materias.Clear();

        ActionResult resultado = cenario.Controller.Confirmar(fluxoId);

        AssertRedirecionamentoComFluxo(resultado, nameof(ProvaController.SelecionarQuestoes), fluxoId);
        Assert.AreEqual(
            "A matéria selecionada não pertence à disciplina informada.",
            cenario.Controller.TempData["MensagemErro"]
        );
    }

    [TestMethod]
    public void Confirmar_QuestaoRemovidaEntrePreviaEConfirmacao_RetornaASelecaoComMensagem()
    {
        Cenario cenario = CriarCenario();
        Guid fluxoId = PrepararEstado(cenario);

        ActionResult previa = cenario.Controller.Confirmar(fluxoId);
        Assert.IsInstanceOfType<ViewResult>(previa);
        cenario.Questoes.Clear();

        ActionResult resultado = cenario.Controller.Confirmar(FormularioVazio(), fluxoId);

        AssertRedirecionamentoComFluxo(resultado, nameof(ProvaController.SelecionarQuestoes), fluxoId);
        Assert.AreEqual(
            "Uma ou mais questões confirmadas não pertencem à configuração da prova.",
            cenario.Controller.TempData["MensagemErro"]
        );
    }

    [TestMethod]
    public void Confirmar_IdsRepetidosNoEstado_RetornaASelecaoSemPersistir()
    {
        Cenario cenario = CriarCenario(quantidadeQuestoes: 2);
        Guid fluxoId = Guid.CreateVersion7();
        Guid idRepetido = cenario.Questoes[0].Id;
        cenario.Controller.TempData[EstadoKey(fluxoId)] = SerializarEstado(
            cenario,
            [idRepetido, idRepetido]
        );

        ActionResult resultado = cenario.Controller.Confirmar(FormularioVazio(), fluxoId);

        AssertRedirecionamentoComFluxo(resultado, nameof(ProvaController.SelecionarQuestoes), fluxoId);
        Assert.AreEqual(MensagemEstadoInvalido, cenario.Controller.TempData["MensagemErro"]);
        cenario.RepositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Confirmar_IdAdulteradoComQuestaoDeOutraMateria_RetornaASelecaoSemPersistir()
    {
        Cenario cenario = CriarCenario();
        var outraMateria = new Materia("Geometria", 7, cenario.Disciplina);
        var questaoAdulterada = CriarQuestao("Quantos lados tem um triângulo?", outraMateria, 100);
        cenario.Materias.Add(outraMateria);
        cenario.Questoes.Add(questaoAdulterada);
        Guid fluxoId = Guid.CreateVersion7();
        cenario.Controller.TempData[EstadoKey(fluxoId)] = SerializarEstado(
            cenario,
            [questaoAdulterada.Id]
        );

        ActionResult resultado = cenario.Controller.Confirmar(FormularioVazio(), fluxoId);

        AssertRedirecionamentoComFluxo(resultado, nameof(ProvaController.SelecionarQuestoes), fluxoId);
        Assert.AreEqual(
            "Uma ou mais questões confirmadas não pertencem à configuração da prova.",
            cenario.Controller.TempData["MensagemErro"]
        );
        cenario.RepositorioProva.Verify(r => r.Cadastrar(It.IsAny<Prova>()), Times.Never);
    }

    [TestMethod]
    public void Confirmar_FalhaDePersistencia_MantemPreviaEEstadoComMensagemAmigavel()
    {
        Cenario cenario = CriarCenario();
        Guid fluxoId = PrepararEstado(cenario);
        cenario.RepositorioProva
            .Setup(r => r.Cadastrar(It.IsAny<Prova>()))
            .Throws(new DbUpdateException("Falha simulada de persistência."));

        ActionResult resultado = cenario.Controller.Confirmar(FormularioVazio(), fluxoId);

        ViewResult view = Assert.IsInstanceOfType<ViewResult>(resultado);
        ConfirmarProvaViewModel modelo = Assert.IsInstanceOfType<ConfirmarProvaViewModel>(view.Model);
        Assert.AreEqual("Avaliação de Álgebra", modelo.Titulo);
        Assert.HasCount(1, modelo.Questoes);
        Assert.AreEqual(
            "Não foi possível salvar a prova. Tente confirmar novamente.",
            cenario.Controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage
        );
        Assert.IsNotNull(cenario.Controller.TempData.Peek(EstadoKey(fluxoId)));
    }

    [TestMethod]
    public void SelecionarQuestoes_SessentaQuestoes_MantemEstadoDentroDoOrcamentoDeCookie()
    {
        Cenario cenario = CriarCenario(Prova.QuantidadeMaximaQuestoes);
        RedirectToActionResult cadastro = Assert.IsInstanceOfType<RedirectToActionResult>(
            cenario.Controller.Cadastrar(new("Avaliação extensa", cenario.Disciplina.Id, 7, false))
        );
        Guid fluxoId = AssertFluxo(cadastro);
        var selecao = new CadastrarProvaEtapa2ViewModel(
            string.Empty,
            string.Empty,
            0,
            false,
            cenario.Materia.Id,
            Prova.QuantidadeMaximaQuestoes
        );

        ActionResult resultado = cenario.Controller.SelecionarQuestoes(selecao, fluxoId);

        AssertRedirecionamentoComFluxo(resultado, nameof(ProvaController.Confirmar), fluxoId);
        string estado = Assert.IsInstanceOfType<string>(cenario.Controller.TempData.Peek(EstadoKey(fluxoId)));
        int bytesJson = Encoding.UTF8.GetByteCount(estado);
        int tamanhoProtegidoEstimado = ((bytesJson + 256 + 2) / 3) * 4;
        Assert.IsLessThanOrEqualTo(4096, tamanhoProtegidoEstimado);
    }

    private static Cenario CriarCenario(int quantidadeQuestoes = 1)
    {
        var disciplina = new Disciplina("Matemática");
        var materia = new Materia("Álgebra", 7, disciplina);
        List<Disciplina> disciplinas = [disciplina];
        List<Materia> materias = [materia];
        List<Questao> questoes = [.. Enumerable.Range(1, quantidadeQuestoes)
            .Select(indice => CriarQuestao($"Questão de álgebra {indice}", materia, indice))];
        List<Prova> provas = [];

        Mock<IRepositorioDisciplina> repositorioDisciplina = new();
        Mock<IRepositorioMateria> repositorioMateria = new();
        Mock<IRepositorioQuestao> repositorioQuestao = new();
        Mock<IRepositorioProva> repositorioProva = new();
        ConfigurarSelecao(repositorioDisciplina, disciplinas);
        ConfigurarSelecao(repositorioMateria, materias);
        ConfigurarSelecao(repositorioQuestao, questoes);
        ConfigurarSelecao(repositorioProva, provas);
        repositorioDisciplina
            .Setup(r => r.SelecionarPorId(It.IsAny<Guid>()))
            .Returns((Guid id) => disciplinas.SingleOrDefault(d => d.Id == id));
        repositorioMateria
            .Setup(r => r.SelecionarPorId(It.IsAny<Guid>()))
            .Returns((Guid id) => materias.SingleOrDefault(m => m.Id == id));
        repositorioProva
            .Setup(r => r.SelecionarPorId(It.IsAny<Guid>()))
            .Returns((Guid id) => provas.SingleOrDefault(p => p.Id == id));

        var servico = new ServicoProva(
            repositorioProva.Object,
            repositorioDisciplina.Object,
            repositorioMateria.Object,
            repositorioQuestao.Object
        );
        Mock<IMapper> mapeador = new();
        mapeador
            .Setup(m => m.Map<List<QuestaoProvaViewModel>>(It.IsAny<object>()))
            .Returns((object origem) => ((List<QuestaoProvaDto>)origem)
                .Select(q => new QuestaoProvaViewModel(
                    q.Id,
                    q.Enunciado,
                    q.Alternativas.Select(a => new AlternativaProvaViewModel(a.Id, a.Texto, a.Correta)).ToList()
                ))
                .ToList());

        var controller = new ProvaController(servico, mapeador.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new ProvedorTempData())
        };

        return new Cenario(
            controller,
            disciplina,
            materia,
            disciplinas,
            materias,
            questoes,
            repositorioProva
        );
    }

    private static Guid PrepararEstado(Cenario cenario)
    {
        Guid fluxoId = Guid.CreateVersion7();
        cenario.Controller.TempData[EstadoKey(fluxoId)] = SerializarEstado(
            cenario,
            [cenario.Questoes[0].Id]
        );
        return fluxoId;
    }

    private static string SerializarConfiguracao(Disciplina disciplina)
    {
        return JsonSerializer.Serialize(new
        {
            Titulo = "Avaliação de Álgebra",
            DisciplinaId = disciplina.Id,
            Serie = 7,
            ProvaRecuperacao = false
        });
    }

    private static string SerializarEstado(Cenario cenario, List<Guid> ids)
    {
        return JsonSerializer.Serialize(new
        {
            Titulo = "Avaliação de Álgebra",
            DisciplinaId = cenario.Disciplina.Id,
            MateriaId = cenario.Materia.Id,
            Serie = 7,
            QuantidadeQuestoes = ids.Count,
            ProvaRecuperacao = false,
            QuestaoIds = ids
        });
    }

    private static Questao CriarQuestao(string enunciado, Materia materia, int indice)
    {
        return new Questao(enunciado, materia,
        [
            new Alternativa($"Correta {indice}", true),
            new Alternativa($"Incorreta {indice}", false)
        ]);
    }

    private static IFormCollection FormularioVazio() =>
        new FormCollection(new Dictionary<string, StringValues>());

    private static void AssertRedirecionamento(ActionResult resultado, string action)
    {
        RedirectToActionResult redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(action, redirect.ActionName);
    }

    private static void AssertRedirecionamentoComFluxo(ActionResult resultado, string action, Guid fluxoId)
    {
        RedirectToActionResult redirect = Assert.IsInstanceOfType<RedirectToActionResult>(resultado);
        Assert.AreEqual(action, redirect.ActionName);
        Assert.AreEqual(fluxoId, AssertFluxo(redirect));
    }

    private static Guid AssertFluxo(RedirectToActionResult redirect)
    {
        Assert.IsNotNull(redirect.RouteValues);
        Assert.IsTrue(redirect.RouteValues.TryGetValue("fluxoId", out object? valor));
        return Assert.IsInstanceOfType<Guid>(valor);
    }

    private static string ConfiguracaoKey(Guid fluxoId) => $"Prova.Configuracao.{fluxoId:N}";
    private static string EstadoKey(Guid fluxoId) => $"Prova.EstadoGeracao.{fluxoId:N}";

    private static void ConfigurarSelecao<TRepositorio, TEntidade>(
        Mock<TRepositorio> repositorio,
        List<TEntidade> registros
    )
        where TRepositorio : class, IRepositorio<TEntidade>
        where TEntidade : EntidadeBase<TEntidade>
    {
        repositorio
            .Setup(r => r.SelecionarTodos(It.IsAny<Expression<Func<TEntidade, bool>>?>()))
            .Returns((Expression<Func<TEntidade, bool>>? filtro) =>
                [.. registros.Where(filtro?.Compile() ?? (_ => true))]);
    }

    private sealed record Cenario(
        ProvaController Controller,
        Disciplina Disciplina,
        Materia Materia,
        List<Disciplina> Disciplinas,
        List<Materia> Materias,
        List<Questao> Questoes,
        Mock<IRepositorioProva> RepositorioProva
    );

    private sealed class ProvedorTempData : ITempDataProvider
    {
        private readonly Dictionary<string, object> valores = [];

        public IDictionary<string, object> LoadTempData(HttpContext context) => valores;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            valores.Clear();

            foreach ((string key, object value) in values)
                valores[key] = value;
        }
    }
}
