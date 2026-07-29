using AutoMapper;
using GeradorDeProvas.Aplicacao.ModuloQuestao;

namespace GeradorDeProvas.WebApp.ModuloQuestao;

public class QuestaoProfile : Profile
{
    public QuestaoProfile()
    {
        CreateMap<ListarQuestaoDto, ListarQuestaoViewModel>();
        CreateMap<AlternativaViewModel, CadastrarAlternativaDto>();
        CreateMap<CadastrarQuestaoViewModel, CadastrarQuestaoDto>();
        CreateMap<EditarQuestaoViewModel, EditarQuestaoDto>();
        CreateMap<AlternativaDto, AlternativaViewModel>();
        CreateMap<DetalhesQuestaoDto, EditarQuestaoViewModel>();
        CreateMap<DetalhesQuestaoDto, ExcluirQuestaoViewModel>();
    }
}
