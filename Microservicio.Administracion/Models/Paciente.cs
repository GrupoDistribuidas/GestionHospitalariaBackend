namespace Microservicio.ClinicaExtension.Models
{
    public class Paciente
    {
        public int IdPaciente { get; set; }
        public required string Nombre { get; set; }
        public required string Cedula { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }

}
