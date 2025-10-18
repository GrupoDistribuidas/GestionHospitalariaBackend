using Microservicio.Consultas.Data;

namespace Microservicio.Consultas.Services
{
    public interface IConsultasDbContextFactory
    {
        ConsultasDbContext CreateForCentro(int idCentroMedico);
    }
}
