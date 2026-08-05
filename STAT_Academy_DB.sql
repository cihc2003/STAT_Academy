CREATE DATABASE STAT_Academy;
GO

USE STAT_Academy;
GO

-- =========================
-- TABLA: TIPO_USUARIO
-- =========================
CREATE TABLE TIPO_USUARIO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(100),
    Estado BIT
);

-- =========================
-- TABLA: USUARIO
-- =========================
CREATE TABLE USUARIO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Email VARCHAR(150),
    Password VARCHAR(255),
    Estado BIT,
    Intentos_Login INT,
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Ultimo_Login DATETIME,
    FK_Tipo_Usuario INT,

    CONSTRAINT FK_USUARIO_TIPO
    FOREIGN KEY (FK_Tipo_Usuario)
    REFERENCES TIPO_USUARIO(ID)
);

-- =========================
-- TABLA: METODO_PAGO
-- =========================
CREATE TABLE METODO_PAGO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(100),
    Estado BIT
);

-- =========================
-- TABLA: PAGO
-- =========================
CREATE TABLE PAGO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Usuario INT,
    FK_Metodo_Pago INT,
    Monto DECIMAL(10,2),
    Fecha_Pago DATETIME,
    Estado BIT,

    CONSTRAINT FK_PAGO_USUARIO
    FOREIGN KEY (FK_Usuario)
    REFERENCES USUARIO(ID),

    CONSTRAINT FK_PAGO_METODO
    FOREIGN KEY (FK_Metodo_Pago)
    REFERENCES METODO_PAGO(ID)
);

-- =========================
-- TABLA: FACTURA
-- =========================
CREATE TABLE FACTURA (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Usuario INT,
    FK_Pago INT,
    Fecha DATETIME,
    Total DECIMAL(10,2),
    Pago BIT,

    CONSTRAINT FK_FACTURA_USUARIO
    FOREIGN KEY (FK_Usuario)
    REFERENCES USUARIO(ID),

    CONSTRAINT FK_FACTURA_PAGO
    FOREIGN KEY (FK_Pago)
    REFERENCES PAGO(ID)
);

-- =========================
-- TABLA: PROVEEDOR
-- =========================
CREATE TABLE PROVEEDOR (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(100),
    Contacto VARCHAR(100),
    Telefono VARCHAR(50),
    Email VARCHAR(100),
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Estado BIT
);

-- =========================
-- TABLA: PRODUCTO
-- =========================
CREATE TABLE PRODUCTO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(100),
    Categoria VARCHAR(100),
    Descripcion VARCHAR(255),
    Precio_Base DECIMAL(10,2),
    Stock INT,
    Min_Stock INT,
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Estado BIT,
    FK_Proveedor INT,

    CONSTRAINT FK_PRODUCTO_PROVEEDOR
    FOREIGN KEY (FK_Proveedor)
    REFERENCES PROVEEDOR(ID)
);

-- =========================
-- TABLA: CURSO
-- =========================
CREATE TABLE CURSO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Tutor INT,
    FK_Creador INT,
    Nombre VARCHAR(100),
    Descripcion VARCHAR(255),
    Precio_Base DECIMAL(10,2),
    Duracion_Semanas INT,
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Estado BIT,
    Fecha_Inicio DATETIME,
    Fecha_Fin DATETIME,

    CONSTRAINT FK_CURSO_TUTOR
    FOREIGN KEY (FK_Tutor)
    REFERENCES USUARIO(ID),

    CONSTRAINT FK_CURSO_CREADOR
    FOREIGN KEY (FK_Creador)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: DETALLE_FACTURA
-- =========================
CREATE TABLE DETALLE_FACTURA (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Factura INT,
    FK_Producto INT NULL,
    FK_Curso INT NULL,
    Cantidad INT,
    Precio_Unitario DECIMAL(10,2),
    Subtotal DECIMAL(10,2),

    CONSTRAINT FK_DETALLE_FACTURA
    FOREIGN KEY (FK_Factura)
    REFERENCES FACTURA(ID),

    CONSTRAINT FK_DETALLE_PRODUCTO
    FOREIGN KEY (FK_Producto)
    REFERENCES PRODUCTO(ID),

    CONSTRAINT FK_DETALLE_CURSO
    FOREIGN KEY (FK_Curso)
    REFERENCES CURSO(ID)
);

-- =========================
-- TABLA: CARRITO
-- =========================
CREATE TABLE CARRITO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Usuario INT,

    CONSTRAINT FK_CARRITO_USUARIO
    FOREIGN KEY (FK_Usuario)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: CARRITO_DETALLE
-- =========================
CREATE TABLE CARRITO_DETALLE (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Carrito INT,
    FK_Producto INT NULL,
    FK_Curso INT NULL,
    Cantidad INT,

    CONSTRAINT FK_CARRITO_DETALLE_CARRITO
    FOREIGN KEY (FK_Carrito)
    REFERENCES CARRITO(ID),

    CONSTRAINT FK_CARRITO_DETALLE_PRODUCTO
    FOREIGN KEY (FK_Producto)
    REFERENCES PRODUCTO(ID),

    CONSTRAINT FK_CARRITO_DETALLE_CURSO
    FOREIGN KEY (FK_Curso)
    REFERENCES CURSO(ID)
);

-- =========================
-- TABLA: ESTUDIANTE_CURSO
-- =========================
CREATE TABLE ESTUDIANTE_CURSO (
    ID_Matricula INT PRIMARY KEY IDENTITY(1,1),
    FK_Curso INT,
    FK_Estudiante INT,
    Fecha_Matricula DATETIME,
    Estado VARCHAR(50),
    Progreso INT,

    CONSTRAINT FK_ESTUDIANTE_CURSO_CURSO
    FOREIGN KEY (FK_Curso)
    REFERENCES CURSO(ID),

    CONSTRAINT FK_ESTUDIANTE_CURSO_USUARIO
    FOREIGN KEY (FK_Estudiante)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: TAREA
-- =========================
CREATE TABLE TAREA (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Curso INT,
    Titulo VARCHAR(100),
    Descripcion VARCHAR(255),
    Fecha_Inicio DATETIME,
    Fecha_Limite DATETIME,
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Entregada BIT,
    Estado BIT,
    FK_Autor INT,

    CONSTRAINT FK_TAREA_CURSO
    FOREIGN KEY (FK_Curso)
    REFERENCES CURSO(ID),

    CONSTRAINT FK_TAREA_AUTOR
    FOREIGN KEY (FK_Autor)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: ENTREGA_TAREA
-- =========================
CREATE TABLE ENTREGA_TAREA (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Tarea INT,
    FK_Estudiante INT,
    Archivo_Entrega VARCHAR(255),
    Fecha_Entrega DATETIME,
    Calificacion DECIMAL(5,2),
    Feedback VARCHAR(255),
    Estado VARCHAR(50),

    CONSTRAINT FK_ENTREGA_TAREA
    FOREIGN KEY (FK_Tarea)
    REFERENCES TAREA(ID),

    CONSTRAINT FK_ENTREGA_ESTUDIANTE
    FOREIGN KEY (FK_Estudiante)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: MATERIAL_CURSO
-- =========================
CREATE TABLE MATERIAL_CURSO (
    ID INT PRIMARY KEY IDENTITY(1,1),
    FK_Curso INT,
    Titulo VARCHAR(100),
    Ubicacion_Material VARCHAR(255),
    Tipo VARCHAR(50),
    Estado BIT,
    FK_Autor INT,
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,

    CONSTRAINT FK_MATERIAL_CURSO
    FOREIGN KEY (FK_Curso)
    REFERENCES CURSO(ID),

    CONSTRAINT FK_MATERIAL_AUTOR
    FOREIGN KEY (FK_Autor)
    REFERENCES USUARIO(ID)
);

-- =========================
-- TABLA: ENTRADA_BLOG
-- =========================
CREATE TABLE ENTRADA_BLOG (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Titulo VARCHAR(150),
    Contenido VARCHAR(MAX),
    Fecha_Creacion DATETIME,
    Fecha_Edicion DATETIME,
    Estado BIT,
    FK_Autor INT,

    CONSTRAINT FK_BLOG_AUTOR
    FOREIGN KEY (FK_Autor)
    REFERENCES USUARIO(ID)
);
-- =========================
-- TABLA: Auditoria
-- =========================
CREATE TABLE AUDITORIA (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    ENTIDAD VARCHAR(100),
    ACCION VARCHAR(50),
    DESCRIPCION VARCHAR(255),
    USUARIO VARCHAR(100),
    FECHA DATETIME
);
ALTER TABLE AUDITORIA
ADD ENTIDAD_ID INT NULL;
---------------------------------------------------------------------------
--  Creación de usuario la base de datos
---------------------------------------------------------------------------

-- 1. Crear login (nivel de servidor)
CREATE LOGIN admin_STAT_Academy WITH PASSWORD = 'admin_STAT_Academy';

-- 2. Crear usuario mapeado para login
CREATE USER admin_STAT_Academy FOR LOGIN admin_STAT_Academy;

-- 3. Agregar al rol de db_owner
ALTER ROLE db_owner ADD MEMBER admin_STAT_Academy;


INSERT INTO TIPO_USUARIO (
    Nombre,
    Estado
)
VALUES (
    'ADMIN',
    1
);

INSERT INTO USUARIO(
    Email ,
    Password,
    Estado,
    Intentos_Login,
    Fecha_Creacion,
    Fecha_Edicion,
    Ultimo_Login,
    FK_Tipo_Usuario
)
VALUES (
    'admin@statacademy.com',
    'Admin123*',
    1,
    0,
    GETDATE(),
    GETDATE(),
    null,
    1
);
INSERT INTO TIPO_USUARIO
(
    Nombre,
    Estado
)
VALUES
(
    'TUTOR',
    1
),
(
    'ESTUDIANTE',
    1
);

SELECT * FROM USUARIO;

SELECT * FROM TIPO_USUARIO;

INSERT INTO USUARIO
(
    Email,
    Password,
    Estado,
    Intentos_Login,
    Fecha_Creacion,
    Fecha_Edicion,
    Ultimo_Login,
    FK_Tipo_Usuario
)
VALUES
(
    'tutor@statacademy.com',
    'Tutor123*',
    1,
    0,
    GETDATE(),
    GETDATE(),
    NULL,
    2
);

INSERT INTO USUARIO
(
    Email,
    Password,
    Estado,
    Intentos_Login,
    Fecha_Creacion,
    Fecha_Edicion,
    Ultimo_Login,
    FK_Tipo_Usuario
)
VALUES
(
    'estudiante@statacademy.com',
    'Estudiante123*',
    1,
    0,
    GETDATE(),
    GETDATE(),
    NULL,
    3
);

SELECT ID, Email, FK_Tipo_Usuario
FROM USUARIO;

INSERT INTO CURSO
(
    FK_Tutor,
    FK_Creador,
    Nombre,
    Descripcion,
    Precio_Base,
    Duracion_Semanas,
    Fecha_Creacion,
    Fecha_Edicion,
    Estado,
    Fecha_Inicio,
    Fecha_Fin
)
VALUES
(
    2,
    1,
    'Programación C# .NET',
    'Curso de introducción a C# y ASP.NET Core',
    50000,
    8,
    GETDATE(),
    GETDATE(),
    1,
    '2026-01-10',
    '2026-03-10'
),
(
    2,
    1,
    'Base de Datos SQL Server',
    'Diseño y administración de bases de datos',
    45000,
    6,
    GETDATE(),
    GETDATE(),
    1,
    '2026-02-01',
    '2026-03-15'
);

INSERT INTO ESTUDIANTE_CURSO
(
    FK_Curso,
    FK_Estudiante,
    Fecha_Matricula,
    Estado,
    Progreso
)
VALUES
(
    1,
    3,
    GETDATE(),
    'Activo',
    0
);
