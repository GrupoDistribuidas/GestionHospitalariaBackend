namespace Microservicio.Administracion.Models
{
    public class CentroMedico
    {
        public int IdCentroMedico { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Ciudad { get; set; }
        public string? Direccion { get; set; }
        public ICollection<Empleado>? Empleados { get; set; }
    }
}
