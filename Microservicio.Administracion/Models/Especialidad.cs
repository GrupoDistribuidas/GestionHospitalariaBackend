namespace Microservicio.Administracion.Models
{
    public class Especialidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public ICollection<Empleado>? Empleados { get; set; }
    }
}
