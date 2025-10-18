CREATE DATABASE IF NOT EXISTS hospital_central;
USE hospital_central;

CREATE TABLE IF NOT EXISTS Centros_Medicos (
    id_centro_medico INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL,
    ciudad VARCHAR(100),
    direccion VARCHAR(200)
);

CREATE TABLE IF NOT EXISTS Tipos_Empleados (
    id_tipo INT AUTO_INCREMENT PRIMARY KEY,
    tipo VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS Especialidades (
    id_especialidad INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS Empleados (
    id_empleado INT AUTO_INCREMENT PRIMARY KEY,
    id_centro_medico INT,
    id_tipo INT,
    id_especialidad INT,
    nombre VARCHAR(100) NOT NULL,
    telefono VARCHAR(20),
    email VARCHAR(100),
    salario DECIMAL(10,2),
    horario VARCHAR(100),
    estado ENUM('Activo','Inactivo') DEFAULT 'Activo',
    FOREIGN KEY (id_centro_medico) REFERENCES Centros_Medicos(id_centro_medico),
    FOREIGN KEY (id_tipo) REFERENCES Tipos_Empleados(id_tipo),
    FOREIGN KEY (id_especialidad) REFERENCES Especialidades(id_especialidad)
);

CREATE TABLE IF NOT EXISTS Usuarios (
    id_usuario INT AUTO_INCREMENT PRIMARY KEY,
    nombre_usuario VARCHAR(50) UNIQUE NOT NULL,
    contrasena VARCHAR(255) NOT NULL,
    id_empleado INT,
    FOREIGN KEY (id_empleado) REFERENCES Empleados(id_empleado)
);
