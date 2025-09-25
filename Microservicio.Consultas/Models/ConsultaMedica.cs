namespace Microservicio.Consultas.Models
{
    public class ConsultaMedica
    {
        public int IdConsultaMedica { get; set; }
        public DateTime Fecha { get; set; }
        public TimeSpan Hora { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
        public string? Tratamiento { get; set; }
        public int IdPaciente { get; set; }
        public int? IdMedico { get; set; }
    }
}
