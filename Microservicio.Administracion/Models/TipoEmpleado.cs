namespace Microservicio.Administracion.Models
{
    public class TipoEmpleado
    {
        public int IdTipo { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public ICollection<Empleado>? Empleados { get; set; }
    }
}
