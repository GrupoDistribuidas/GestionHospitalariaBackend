using Microservicio.Consultas.Data;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Consultas.Services
{
    public class ConsultasDbContextFactory : IConsultasDbContextFactory
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConsultasDbContextFactory> _logger;

        public ConsultasDbContextFactory(IConfiguration configuration, ILogger<ConsultasDbContextFactory> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public ConsultasDbContext CreateForCentro(int idCentroMedico)
        {
            string connName = idCentroMedico switch
            {
                2 => "ClinicaExtension_Guayaquil",
                3 => "ClinicaExtension_Cuenca",
                _ => "DefaultConnection"
            };

            var conn = _configuration.GetConnectionString(connName);
            if (string.IsNullOrEmpty(conn))
            {
                _logger.LogError("Connection string '{ConnName}' no encontrada para centro {Centro}", connName, idCentroMedico);
                throw new InvalidOperationException($"Connection string '{connName}' no encontrada");
            }

            _logger.LogInformation("Creando ConsultasDbContext para centro {Centro} usando '{ConnName}'", idCentroMedico, connName);

            var options = new DbContextOptionsBuilder<ConsultasDbContext>()
                .UseMySql(conn, ServerVersion.AutoDetect(conn))
                .Options;

            return new ConsultasDbContext(options);
        }
    }
}
