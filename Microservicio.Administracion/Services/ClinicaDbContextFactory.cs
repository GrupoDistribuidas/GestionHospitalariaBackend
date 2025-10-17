using Microservicio.ClinicaExtension.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microservicio.Administracion.Services
{
    public class ClinicaDbContextFactory : IClinicaDbContextFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClinicaDbContextFactory> _logger;

        public ClinicaDbContextFactory(IConfiguration configuration, ILogger<ClinicaDbContextFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ClinicaExtensionDbContext CreateForCentro(int idCentroMedico)
        {
            // Mapear idCentroMedico a connection string. Asumimos:
            // id 1 -> hosp_central (no extension)
            // id 2 -> extension_1 (Guayaquil)
            // id 3 -> extension_2 (Cuenca)
            string connName = idCentroMedico switch
            {
                2 => "ClinicaExtension_Guayaquil",
                3 => "ClinicaExtension_Cuenca",
                _ => "ClinicaExtension" // por defecto la central o fallback
            };

            var conn = _configuration.GetConnectionString(connName);
            if (string.IsNullOrEmpty(conn))
            {
                _logger.LogError("Connection string '{ConnName}' no encontrada para centro {Centro}", connName, idCentroMedico);
                throw new InvalidOperationException($"Connection string '{connName}' no encontrada");
            }

            _logger.LogInformation("Creando ClinicaExtensionDbContext para centro {Centro} usando '{ConnName}'", idCentroMedico, connName);

            var options = new DbContextOptionsBuilder<ClinicaExtensionDbContext>()
                .UseMySql(conn, ServerVersion.AutoDetect(conn))
                .Options;

            return new ClinicaExtensionDbContext(options);
        }
    }
}
