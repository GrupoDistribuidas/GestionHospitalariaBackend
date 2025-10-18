CREATE DATABASE IF NOT EXISTS extension_1;
USE extension_1;

-- Tabla Pacientes
CREATE TABLE IF NOT EXISTS Pacientes (
    id_paciente INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    cedula VARCHAR(20) UNIQUE NOT NULL,
    fecha_nacimiento DATE NOT NULL,
    telefono VARCHAR(20),
    direccion VARCHAR(150)
);

-- Tabla Consulta_medica
CREATE TABLE IF NOT EXISTS Consulta_medica (
    id_consulta_medica INT AUTO_INCREMENT PRIMARY KEY,
    fecha DATE NOT NULL,
    hora TIME NOT NULL,
    motivo TEXT NOT NULL,
    diagnostico TEXT,
    tratamiento TEXT,
    id_paciente INT NOT NULL,
    id_medico INT,
    CONSTRAINT fk_consulta_paciente
        FOREIGN KEY (id_paciente) REFERENCES Pacientes(id_paciente)
        ON DELETE CASCADE ON UPDATE CASCADE
);
