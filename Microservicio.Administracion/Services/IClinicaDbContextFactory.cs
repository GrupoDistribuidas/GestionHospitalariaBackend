using Microservicio.ClinicaExtension.Data;

namespace Microservicio.Administracion.Services
{
    public interface IClinicaDbContextFactory
    {
        ClinicaExtensionDbContext CreateForCentro(int idCentroMedico);
    }
}
