namespace Microservicio.Administracion.Models
{
    public class Especialidad
    {
        public int IdEspecialidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<Empleado>? Empleados { get; set; }
    }
}
