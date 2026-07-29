using GeradorDeProvas.Dominio.Compartilhado.Identity;

namespace GeradorDeProvas.Testes.Integracao.Compartilhado.Identity;

public sealed class FalsoProvedorDeUsuario(Guid userId) : IProvedorDeUsuario
{
    public Guid? Id => userId;
    public bool EstaAutenticado => true;
}
