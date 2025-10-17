CREATE DATABASE IF NOT EXISTS hosp_central;
USE hosp_central;

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
    rol ENUM('Admin','Usuario') DEFAULT 'Usuario',
    CONSTRAINT fk_usuario_empleado FOREIGN KEY (id_empleado) REFERENCES Empleados(id_empleado)
);

-- Inserciones iniciales
INSERT INTO Centros_Medicos (nombre, ciudad, direccion) 
VALUES ('Hospital Central Quito', 'Quito', 'Av. Amazonas N34-122');

INSERT INTO Centros_Medicos (nombre, ciudad, direccion) 
VALUES ('Clínica Vida Sana', 'Guayaquil', 'Malecón 2000 y Loja');

INSERT INTO Centros_Medicos (nombre, ciudad, direccion) 
VALUES ('Hospital del Norte', 'Cuenca', 'Av. de las Américas 102');

INSERT INTO Centros_Medicos (nombre, ciudad, direccion) 
VALUES ('Centro Médico Tungurahua', 'Ambato', 'Bolívar y Cevallos');

INSERT INTO Centros_Medicos (nombre, ciudad, direccion) 
VALUES ('Clínica Los Andes', 'Loja', 'Av. Universitaria 202');

-- ==============================
-- Inserciones en Tipos_Empleados
-- ==============================
INSERT INTO Tipos_Empleados (tipo) VALUES ('Médico');
INSERT INTO Tipos_Empleados (tipo) VALUES ('Enfermero');
INSERT INTO Tipos_Empleados (tipo) VALUES ('Administrador');
INSERT INTO Tipos_Empleados (tipo) VALUES ('Recepcionista');
INSERT INTO Tipos_Empleados (tipo) VALUES ('Técnico de Laboratorio');

-- ==============================
-- Inserciones en Especialidades
-- ==============================
INSERT INTO Especialidades (nombre) VALUES ('Cardiología');
INSERT INTO Especialidades (nombre) VALUES ('Pediatría');
INSERT INTO Especialidades (nombre) VALUES ('Cirugía General');
INSERT INTO Especialidades (nombre) VALUES ('Radiología');
INSERT INTO Especialidades (nombre) VALUES ('Medicina Interna');

-- ==============================
-- Inserciones en Empleados
-- ==============================
INSERT INTO Empleados (id_centro_medico, id_tipo, id_especialidad, nombre, telefono, email, salario, horario, estado)
VALUES (1, 1, 1, 'Dr. Juan Pérez', '0987654321', 'juan.perez@hospitalcentral.com', 2500.00, '08:00 - 16:00', 'Activo');

INSERT INTO Empleados (id_centro_medico, id_tipo, id_especialidad, nombre, telefono, email, salario, horario, estado)
VALUES (2, 2, 2, 'Enf. María López', '0976543210', 'maria.lopez@clinica.com', 1200.00, '07:00 - 15:00', 'Activo');

INSERT INTO Empleados (id_centro_medico, id_tipo, id_especialidad, nombre, telefono, email, salario, horario, estado)
VALUES (3, 3, 3, 'Carlos Andrade', '0965432109', 'carlos.andrade@hospitalnorte.com', 1800.00, '09:00 - 17:00', 'Activo');

INSERT INTO Empleados (id_centro_medico, id_tipo, id_especialidad, nombre, telefono, email, salario, horario, estado)
VALUES (1, 4, 4, 'Ana Torres', '0954321098', 'ana.torres@centromedico.com', 1000.00, '10:00 - 18:00', 'Activo');

INSERT INTO Empleados (id_centro_medico, id_tipo, id_especialidad, nombre, telefono, email, salario, horario, estado)
VALUES (1, 5, 5, 'Luis Gomez', '0943210987', 'luis.gomez@clinicaandes.com', 1500.00, '12:00 - 20:00', 'Activo');

-- ==============================
-- Inserciones en Usuarios
-- ==============================
INSERT INTO Usuarios (nombre_usuario, contrasena, id_empleado, rol) 
VALUES ('jperez', '$2a$12$gf3MrJjH7B34QhLM/hrcdOVGh03OKzxy6VcTBhz.yK4XwHtIcwwR2', 1, 'Usuario');

INSERT INTO Usuarios (nombre_usuario, contrasena, id_empleado, rol) 
VALUES ('mlopez', '$2a$12$gf3MrJjH7B34QhLM/hrcdOVGh03OKzxy6VcTBhz.yK4XwHtIcwwR2', 2, 'Usuario');

INSERT INTO Usuarios (nombre_usuario, contrasena, id_empleado, rol) 
VALUES ('atorres', '$2a$12$gf3MrJjH7B34QhLM/hrcdOVGh03OKzxy6VcTBhz.yK4XwHtIcwwR2', 4, 'Usuario');

INSERT INTO Usuarios (nombre_usuario, contrasena, id_empleado, rol) 
VALUES ('lgomez', '$2a$12$gf3MrJjH7B34QhLM/hrcdOVGh03OKzxy6VcTBhz.yK4XwHtIcwwR2', 5, 'Admin');
