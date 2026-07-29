using AutoMapper;
using GeradorDeProvas.Aplicacao.ModuloDisciplina;

namespace GeradorDeProvas.WebApp.ModuloDisciplina;

public class DisciplinaProfile : Profile
{
    public DisciplinaProfile()
    {
        CreateMap<ListarDisciplinaDto, ListarDisciplinaViewModel>();
        CreateMap<CadastrarDisciplinaViewModel, CadastrarDisciplinaDto>();
        CreateMap<EditarDisciplinaViewModel, EditarDisciplinaDto>();
        CreateMap<DetalhesDisciplinaDto, EditarDisciplinaViewModel>();
        CreateMap<DetalhesDisciplinaDto, ExcluirDisciplinaViewModel>();
    }
}
