using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    public class ReporteConsultasRequest
    {
        /// <summary>
        /// ID del médico específico (opcional)
        /// </summary>
        public int? IdMedico { get; set; }

        /// <summary>
        /// Fecha de inicio del rango en formato yyyy-MM-dd (opcional)
        /// </summary>
        public string? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del rango en formato yyyy-MM-dd (opcional)
        /// </summary>
        public string? FechaFin { get; set; }

        /// <summary>
        /// Filtro por motivo de la consulta (opcional)
        /// </summary>
        public string? Motivo { get; set; }

        /// <summary>
        /// Filtro por diagnóstico de la consulta (opcional)
        /// </summary>
        public string? Diagnostico { get; set; }
    }

    public class EstadisticasConsultasRequest
    {
        /// <summary>
        /// Fecha de inicio del rango en formato yyyy-MM-dd (opcional)
        /// </summary>
        public string? FechaInicio { get; set; }

        /// <summary>
        /// Fecha de fin del rango en formato yyyy-MM-dd (opcional)
        /// </summary>
        public string? FechaFin { get; set; }
    }

    public class ReporteConsultasResponse
    {
        public ResumenReporte Resumen { get; set; } = new();
        public List<MedicoReporte> Medicos { get; set; } = new();
    }

    public class ResumenReporte
    {
        public int TotalConsultasGeneral { get; set; }
        public int TotalMedicos { get; set; }
        public string FechaGeneracion { get; set; } = string.Empty;
        public FiltrosReporte Filtros { get; set; } = new();
    }

    public class FiltrosReporte
    {
        public int? MedicoId { get; set; }
        public string? FechaInicio { get; set; }
        public string? FechaFin { get; set; }
        public string? Motivo { get; set; }
        public string? Diagnostico { get; set; }
    }

    public class MedicoReporte
    {
        public int IdMedico { get; set; }
        public string NombreMedico { get; set; } = string.Empty;
        public int IdEspecialidad { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public int TotalConsultas { get; set; }
        public List<ConsultaReporte> Consultas { get; set; } = new();
    }

    public class ConsultaReporte
    {
        public int IdConsulta { get; set; }
        public string Fecha { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public string Diagnostico { get; set; } = string.Empty;
        public string Tratamiento { get; set; } = string.Empty;
        public PacienteReporte Paciente { get; set; } = new();
    }

    public class PacienteReporte
    {
        public int IdPaciente { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;
    }

    public class EstadisticasConsultasResponse
    {
        public int TotalConsultas { get; set; }
        public int TotalMedicos { get; set; }
        public string FechaGeneracion { get; set; } = string.Empty;
        public double PromedioConsultasPorMedico { get; set; }
        public string? MedicoConMasConsultas { get; set; }
        public int MaxConsultasPorMedico { get; set; }
        public List<EspecialidadEstadistica> Especialidades { get; set; } = new();
    }

    public class EspecialidadEstadistica
    {
        public int IdEspecialidad { get; set; }
        public string NombreEspecialidad { get; set; } = string.Empty;
        public int TotalMedicos { get; set; }
        public int TotalConsultas { get; set; }
    }
}