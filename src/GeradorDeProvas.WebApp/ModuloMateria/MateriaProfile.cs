using AutoMapper;
using GeradorDeProvas.Aplicacao.ModuloMateria;

namespace GeradorDeProvas.WebApp.ModuloMateria;

public class MateriaProfile : Profile
{
    public MateriaProfile()
    {
        CreateMap<ListarMateriaDto, ListarMateriaViewModel>();
        CreateMap<CadastrarMateriaViewModel, CadastrarMateriaDto>();
        CreateMap<EditarMateriaViewModel, EditarMateriaDto>();
        CreateMap<DetalhesMateriaDto, EditarMateriaViewModel>();
        CreateMap<DetalhesMateriaDto, ExcluirMateriaViewModel>();
    }
}
