namespace Microservicio.Administracion.Models
{
    public class Empleado
    {
        public int IdEmpleado { get; set; }
        public int? IdCentroMedico { get; set; }
        public int? IdTipo { get; set; }
        public int? IdEspecialidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public decimal? Salario { get; set; }
        public string? Horario { get; set; }
        public string Estado { get; set; } = "Activo";

        public CentroMedico? CentroMedico { get; set; }
        public TipoEmpleado? TipoEmpleado { get; set; }
        public Especialidad? Especialidad { get; set; }
    }
}
