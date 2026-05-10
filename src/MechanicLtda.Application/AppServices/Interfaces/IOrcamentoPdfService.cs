using MechanicLtda.Application.DTOs;

namespace MechanicLtda.Application.AppServices.Interfaces
{
    public interface IOrcamentoPdfAppService
    {
        byte[] Gerar(OrcamentoPdfDto dto);
    }
}