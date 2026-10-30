/* =====================================================================
   HUELLA DE CARBONO (HDC) - Script ÚNICO de base de datos
   ---------------------------------------------------------------------
   Reemplaza a los antiguos 01_esquema.sql, 02_datos_iniciales.sql y
   03_indices.sql. Contiene, en este orden:
     1. Esquema (tablas)
     2. Datos iniciales (roles, permisos y catálogos)
     3. Índices de rendimiento

   Es IDEMPOTENTE: cada tabla, dato inicial e índice se crea solo si no
   existe. Por eso la API (InicializadorBaseDatos) lo ejecuta en CADA
   arranque: en una base vacía crea todo, y en una base existente solo
   agrega lo que falte (por ejemplo, un índice nuevo).

   Uso manual (SSMS / sqlcmd): crear la base y ejecutarlo dentro de ella:
       CREATE DATABASE HuellaCarbono;   -- si no existe
       USE HuellaCarbono;
       -- y luego ejecutar este archivo completo

   Notas del modelo (correcciones sobre el script original):
   - PK con IDENTITY (excepto PROVINCIA/DISTRITO/CIUDAD que usan el id de georef)
   - CIUDAD.NOMBRE pasa de BIGINT a NVARCHAR
   - FECHA_BAJA admite NULL (registro activo = sin fecha de baja)
   - REGISTROS pasa a ser cabecera (domicilio + período) con DETALLE_REGISTRO
   - ROL_PERMISO (N:M), REPORTE
   - VEHICULO se eliminó (si existe en una base anterior, se borra)
   - COLABORA se reemplaza por COLABORACION + COLABORACION_TIPO_EMISION
   ===================================================================== */


/* =====================================================================
   1. ESQUEMA
   ===================================================================== */

IF OBJECT_ID(N'ROL', N'U') IS NULL
CREATE TABLE ROL
(
    ID_ROL       BIGINT IDENTITY(1,1) NOT NULL,
    NOMBRE       NVARCHAR(200) NOT NULL,
    DESCRIPCION  NVARCHAR(500) NULL,
    ES_SISTEMA   BIT NOT NULL CONSTRAINT DF_ROL_ES_SISTEMA DEFAULT (0),
    CONSTRAINT PK_ROL PRIMARY KEY (ID_ROL),
    CONSTRAINT UQ_ROL_NOMBRE UNIQUE (NOMBRE)
);
GO

IF OBJECT_ID(N'PERMISOS', N'U') IS NULL
CREATE TABLE PERMISOS
(
    ID_PERMISO   BIGINT IDENTITY(1,1) NOT NULL,
    NOMBRE       NVARCHAR(250) NOT NULL,
    DESCRIPCION  NVARCHAR(500) NULL,
    CONSTRAINT PK_PERMISOS PRIMARY KEY (ID_PERMISO),
    CONSTRAINT UQ_PERMISOS_NOMBRE UNIQUE (NOMBRE)
);
GO

IF OBJECT_ID(N'ROL_PERMISO', N'U') IS NULL
CREATE TABLE ROL_PERMISO
(
    ID_ROL       BIGINT NOT NULL,
    ID_PERMISO   BIGINT NOT NULL,
    CONSTRAINT PK_ROL_PERMISO PRIMARY KEY (ID_ROL, ID_PERMISO),
    CONSTRAINT FK_ROL_PERMISO_ROL FOREIGN KEY (ID_ROL) REFERENCES ROL (ID_ROL) ON DELETE CASCADE,
    CONSTRAINT FK_ROL_PERMISO_PERMISO FOREIGN KEY (ID_PERMISO) REFERENCES PERMISOS (ID_PERMISO) ON DELETE CASCADE
);
GO

IF OBJECT_ID(N'PROVINCIA', N'U') IS NULL
CREATE TABLE PROVINCIA
(
    ID_PROVINCIA BIGINT NOT NULL,
    NOMBRE       NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_PROVINCIA PRIMARY KEY (ID_PROVINCIA)
);
GO

IF OBJECT_ID(N'DISTRITO', N'U') IS NULL
CREATE TABLE DISTRITO
(
    ID_DISTRITO  BIGINT NOT NULL,
    ID_PROVINCIA BIGINT NOT NULL,
    NOMBRE       NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_DISTRITO PRIMARY KEY (ID_DISTRITO),
    CONSTRAINT FK_DISTRITO_PROVINCIA FOREIGN KEY (ID_PROVINCIA) REFERENCES PROVINCIA (ID_PROVINCIA)
);
GO

IF OBJECT_ID(N'CIUDAD', N'U') IS NULL
CREATE TABLE CIUDAD
(
    ID_CIUDAD    BIGINT NOT NULL,
    ID_DISTRITO  BIGINT NOT NULL,
    NOMBRE       NVARCHAR(250) NOT NULL,
    CONSTRAINT PK_CIUDAD PRIMARY KEY (ID_CIUDAD),
    CONSTRAINT FK_CIUDAD_DISTRITO FOREIGN KEY (ID_DISTRITO) REFERENCES DISTRITO (ID_DISTRITO)
);
GO

IF OBJECT_ID(N'UNIDAD_MEDIDA', N'U') IS NULL
CREATE TABLE UNIDAD_MEDIDA
(
    ID_UNIDAD_MEDIDA BIGINT IDENTITY(1,1) NOT NULL,
    NOMBRE           NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_UNIDAD_MEDIDA PRIMARY KEY (ID_UNIDAD_MEDIDA),
    CONSTRAINT UQ_UNIDAD_MEDIDA_NOMBRE UNIQUE (NOMBRE)
);
GO

IF OBJECT_ID(N'ALCANCE', N'U') IS NULL
CREATE TABLE ALCANCE
(
    ID_ALCANCE   BIGINT IDENTITY(1,1) NOT NULL,
    NOMBRE       NVARCHAR(200) NOT NULL,
    DESCRIPCION  NVARCHAR(500) NULL,
    CONSTRAINT PK_ALCANCE PRIMARY KEY (ID_ALCANCE),
    CONSTRAINT UQ_ALCANCE_NOMBRE UNIQUE (NOMBRE)
);
GO

IF OBJECT_ID(N'TIPO_EMISION', N'U') IS NULL
CREATE TABLE TIPO_EMISION
(
    ID_TIPO_EMISION BIGINT IDENTITY(1,1) NOT NULL,
    ID_ALCANCE      BIGINT NOT NULL,
    NOMBRE          NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_TIPO_EMISION PRIMARY KEY (ID_TIPO_EMISION),
    CONSTRAINT FK_TIPO_EMISION_ALCANCE FOREIGN KEY (ID_ALCANCE) REFERENCES ALCANCE (ID_ALCANCE)
);
GO

IF OBJECT_ID(N'FUENTE_EMISION', N'U') IS NULL
CREATE TABLE FUENTE_EMISION
(
    ID_FUENTE_EMISION    BIGINT IDENTITY(1,1) NOT NULL,
    ID_UNIDAD_MEDIDA     BIGINT NOT NULL,
    ID_TIPO_EMISION      BIGINT NOT NULL,
    NOMBRE               NVARCHAR(250) NOT NULL,
    VALOR_FACTOR_EMISION DECIMAL(18,6) NOT NULL,
    FECHA_ALTA           SMALLDATETIME NOT NULL CONSTRAINT DF_FUENTE_FECHA_ALTA DEFAULT (GETDATE()),
    FECHA_BAJA           SMALLDATETIME NULL,
    FECHA_MODIFICACION   SMALLDATETIME NULL,
    CONSTRAINT PK_FUENTE_EMISION PRIMARY KEY (ID_FUENTE_EMISION),
    CONSTRAINT FK_FUENTE_UNIDAD FOREIGN KEY (ID_UNIDAD_MEDIDA) REFERENCES UNIDAD_MEDIDA (ID_UNIDAD_MEDIDA),
    CONSTRAINT FK_FUENTE_TIPO FOREIGN KEY (ID_TIPO_EMISION) REFERENCES TIPO_EMISION (ID_TIPO_EMISION),
    CONSTRAINT CK_FUENTE_FACTOR CHECK (VALOR_FACTOR_EMISION >= 0)
);
GO

IF OBJECT_ID(N'USUARIO', N'U') IS NULL
CREATE TABLE USUARIO
(
    ID_USUARIO   BIGINT IDENTITY(1,1) NOT NULL,
    ID_ROL       BIGINT NOT NULL,
    EMAIL        NVARCHAR(100) NOT NULL,
    CONTRASENA   NVARCHAR(250) NOT NULL,
    TELEFONO     VARCHAR(30) NULL,
    FECHA_ALTA   SMALLDATETIME NOT NULL CONSTRAINT DF_USUARIO_FECHA_ALTA DEFAULT (GETDATE()),
    FECHA_BAJA   SMALLDATETIME NULL,
    CONSTRAINT PK_USUARIO PRIMARY KEY (ID_USUARIO),
    CONSTRAINT UQ_USUARIO_EMAIL UNIQUE (EMAIL),
    CONSTRAINT FK_USUARIO_ROL FOREIGN KEY (ID_ROL) REFERENCES ROL (ID_ROL)
);
GO

IF OBJECT_ID(N'INDIVIDUO', N'U') IS NULL
CREATE TABLE INDIVIDUO
(
    ID_USUARIO       BIGINT NOT NULL,
    NOMBRES          NVARCHAR(200) NOT NULL,
    APELLIDOS        NVARCHAR(250) NOT NULL,
    DNI              VARCHAR(10) NOT NULL,
    FECHA_NACIMIENTO DATE NULL,
    GENERO           NVARCHAR(30) NULL,
    CONSTRAINT PK_INDIVIDUO PRIMARY KEY (ID_USUARIO),
    CONSTRAINT UQ_INDIVIDUO_DNI UNIQUE (DNI),
    CONSTRAINT FK_INDIVIDUO_USUARIO FOREIGN KEY (ID_USUARIO) REFERENCES USUARIO (ID_USUARIO) ON DELETE CASCADE
);
GO

IF OBJECT_ID(N'ORGANIZACION', N'U') IS NULL
CREATE TABLE ORGANIZACION
(
    ID_USUARIO        BIGINT NOT NULL,
    RAZON_SOCIAL      NVARCHAR(250) NOT NULL,
    CUIT              VARCHAR(13) NOT NULL,
    AREA              NVARCHAR(100) NOT NULL,
    DESCRIPCION       NVARCHAR(1000) NULL,
    CANTIDAD_MIEMBROS INT NULL,
    CONSTRAINT PK_ORGANIZACION PRIMARY KEY (ID_USUARIO),
    CONSTRAINT UQ_ORGANIZACION_CUIT UNIQUE (CUIT),
    CONSTRAINT FK_ORGANIZACION_USUARIO FOREIGN KEY (ID_USUARIO) REFERENCES USUARIO (ID_USUARIO) ON DELETE CASCADE,
    CONSTRAINT CK_ORGANIZACION_MIEMBROS CHECK (CANTIDAD_MIEMBROS IS NULL OR CANTIDAD_MIEMBROS >= 0)
);
GO

-- La tabla VEHICULO se eliminó del modelo (la sección de vehículos ya no
-- existe). Si quedó creada en una base anterior, se borra al arrancar.
IF OBJECT_ID(N'VEHICULO', N'U') IS NOT NULL
    DROP TABLE VEHICULO;
GO

IF OBJECT_ID(N'DOMICILIO', N'U') IS NULL
CREATE TABLE DOMICILIO
(
    ID_DOMICILIO  BIGINT IDENTITY(1,1) NOT NULL,
    ID_CIUDAD     BIGINT NOT NULL,
    ID_USUARIO    BIGINT NOT NULL,
    CALLE         NVARCHAR(200) NOT NULL,
    NUMERO        INT NOT NULL,
    DEPARTAMENTO  NVARCHAR(20) NULL,
    PISO          NVARCHAR(20) NULL,
    BLOCK         NVARCHAR(20) NULL,
    MANZANA       NVARCHAR(20) NULL,
    CODIGO_POSTAL NVARCHAR(15) NOT NULL,
    TELEFONO      VARCHAR(30) NULL,
    FECHA_ALTA    SMALLDATETIME NOT NULL CONSTRAINT DF_DOMICILIO_FECHA_ALTA DEFAULT (GETDATE()),
    FECHA_BAJA    SMALLDATETIME NULL,
    TOTAL_KG_CO2  DECIMAL(18,6) NOT NULL CONSTRAINT DF_DOMICILIO_TOTAL DEFAULT (0),
    CONSTRAINT PK_DOMICILIO PRIMARY KEY (ID_DOMICILIO),
    CONSTRAINT FK_DOMICILIO_CIUDAD FOREIGN KEY (ID_CIUDAD) REFERENCES CIUDAD (ID_CIUDAD),
    CONSTRAINT FK_DOMICILIO_USUARIO FOREIGN KEY (ID_USUARIO) REFERENCES USUARIO (ID_USUARIO)
);
GO

/* Cabecera del registro de emisiones: un domicilio en un período (mes) */
IF OBJECT_ID(N'REGISTROS', N'U') IS NULL
CREATE TABLE REGISTROS
(
    ID_REGISTRO               BIGINT IDENTITY(1,1) NOT NULL,
    ID_DOMICILIO              BIGINT NOT NULL,
    ID_USUARIO_CARGA          BIGINT NOT NULL,
    PERIODO                   DATE NOT NULL,
    OBSERVACIONES             NVARCHAR(500) NULL,
    TOTAL_KG_CO2              DECIMAL(18,6) NOT NULL CONSTRAINT DF_REGISTROS_TOTAL DEFAULT (0),
    FECHA_CREACION            DATETIME NOT NULL CONSTRAINT DF_REGISTROS_CREACION DEFAULT (GETDATE()),
    FECHA_ULTIMA_MODIFICACION DATETIME NOT NULL CONSTRAINT DF_REGISTROS_MODIF DEFAULT (GETDATE()),
    CONSTRAINT PK_REGISTROS PRIMARY KEY (ID_REGISTRO),
    CONSTRAINT FK_REGISTROS_DOMICILIO FOREIGN KEY (ID_DOMICILIO) REFERENCES DOMICILIO (ID_DOMICILIO),
    CONSTRAINT FK_REGISTROS_USUARIO_CARGA FOREIGN KEY (ID_USUARIO_CARGA) REFERENCES USUARIO (ID_USUARIO),
    CONSTRAINT CK_REGISTROS_PERIODO CHECK (DAY(PERIODO) = 1)
);
GO

/* Detalle: cada consumo de una fuente de emisión dentro del registro */
IF OBJECT_ID(N'DETALLE_REGISTRO', N'U') IS NULL
CREATE TABLE DETALLE_REGISTRO
(
    ID_DETALLE           BIGINT IDENTITY(1,1) NOT NULL,
    ID_REGISTRO          BIGINT NOT NULL,
    ID_FUENTE_EMISION    BIGINT NOT NULL,
    CONSUMO              DECIMAL(18,4) NOT NULL,
    VALOR_FACTOR_EMISION DECIMAL(18,6) NOT NULL,
    KG_CO2               DECIMAL(18,6) NOT NULL,
    CONSTRAINT PK_DETALLE_REGISTRO PRIMARY KEY (ID_DETALLE),
    CONSTRAINT FK_DETALLE_REGISTRO FOREIGN KEY (ID_REGISTRO) REFERENCES REGISTROS (ID_REGISTRO) ON DELETE CASCADE,
    CONSTRAINT FK_DETALLE_FUENTE FOREIGN KEY (ID_FUENTE_EMISION) REFERENCES FUENTE_EMISION (ID_FUENTE_EMISION),
    CONSTRAINT CK_DETALLE_CONSUMO CHECK (CONSUMO > 0)
);
GO

/* Colaboración: el titular del domicilio invita a otro usuario a cargar
   emisiones de ciertos tipos durante un rango de períodos */
IF OBJECT_ID(N'COLABORACION', N'U') IS NULL
CREATE TABLE COLABORACION
(
    ID_COLABORACION        BIGINT IDENTITY(1,1) NOT NULL,
    ID_DOMICILIO           BIGINT NOT NULL,
    ID_USUARIO_COLABORADOR BIGINT NOT NULL,
    PERIODO_DESDE          DATE NOT NULL,
    PERIODO_HASTA          DATE NULL,
    ESTADO                 VARCHAR(20) NOT NULL CONSTRAINT DF_COLABORACION_ESTADO DEFAULT ('PENDIENTE'),
    MENSAJE                NVARCHAR(500) NULL,
    FECHA_INVITACION       DATETIME NOT NULL CONSTRAINT DF_COLABORACION_FECHA DEFAULT (GETDATE()),
    FECHA_RESPUESTA        DATETIME NULL,
    CONSTRAINT PK_COLABORACION PRIMARY KEY (ID_COLABORACION),
    CONSTRAINT FK_COLABORACION_DOMICILIO FOREIGN KEY (ID_DOMICILIO) REFERENCES DOMICILIO (ID_DOMICILIO) ON DELETE CASCADE,
    CONSTRAINT FK_COLABORACION_USUARIO FOREIGN KEY (ID_USUARIO_COLABORADOR) REFERENCES USUARIO (ID_USUARIO),
    CONSTRAINT CK_COLABORACION_ESTADO CHECK (ESTADO IN ('PENDIENTE', 'ACEPTADA', 'RECHAZADA', 'REVOCADA')),
    CONSTRAINT CK_COLABORACION_PERIODOS CHECK (PERIODO_HASTA IS NULL OR PERIODO_HASTA >= PERIODO_DESDE)
);
GO

IF OBJECT_ID(N'COLABORACION_TIPO_EMISION', N'U') IS NULL
CREATE TABLE COLABORACION_TIPO_EMISION
(
    ID_COLABORACION BIGINT NOT NULL,
    ID_TIPO_EMISION BIGINT NOT NULL,
    CONSTRAINT PK_COLABORACION_TIPO PRIMARY KEY (ID_COLABORACION, ID_TIPO_EMISION),
    CONSTRAINT FK_COLABTIPO_COLABORACION FOREIGN KEY (ID_COLABORACION) REFERENCES COLABORACION (ID_COLABORACION) ON DELETE CASCADE,
    CONSTRAINT FK_COLABTIPO_TIPO FOREIGN KEY (ID_TIPO_EMISION) REFERENCES TIPO_EMISION (ID_TIPO_EMISION)
);
GO

/* Reporte guardado: conserva los filtros para volver a generarlo */
IF OBJECT_ID(N'REPORTE', N'U') IS NULL
CREATE TABLE REPORTE
(
    ID_REPORTE     BIGINT IDENTITY(1,1) NOT NULL,
    ID_USUARIO     BIGINT NOT NULL,
    NOMBRE         NVARCHAR(200) NOT NULL,
    DESCRIPCION    NVARCHAR(500) NULL,
    FILTROS        NVARCHAR(MAX) NOT NULL,
    FECHA_CREACION DATETIME NOT NULL CONSTRAINT DF_REPORTE_FECHA DEFAULT (GETDATE()),
    CONSTRAINT PK_REPORTE PRIMARY KEY (ID_REPORTE),
    CONSTRAINT FK_REPORTE_USUARIO FOREIGN KEY (ID_USUARIO) REFERENCES USUARIO (ID_USUARIO) ON DELETE CASCADE
);
GO


/* =====================================================================
   2. DATOS INICIALES (catálogos, roles y permisos)
   ---------------------------------------------------------------------
   Cada bloque se carga solo si su tabla está VACÍA. Así, si el
   administrador después modifica o da de baja algún dato, el script no
   lo vuelve a insertar en el siguiente arranque.

   Provincia / Distrito / Ciudad se cargan desde la API georef al iniciar.
   El usuario administrador se crea desde la API (contraseña con hash BCrypt).
   Los factores de emisión son valores de referencia: el administrador
   debe revisarlos y ajustarlos según la fuente oficial que utilice.
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM ROL)
BEGIN
    SET IDENTITY_INSERT ROL ON;
    INSERT INTO ROL (ID_ROL, NOMBRE, DESCRIPCION, ES_SISTEMA) VALUES
     (1, N'Administrador', N'Gestiona cuentas, factores de emisión y parametrización del sistema', 1),
     (2, N'Individuo',     N'Persona que mide su huella de carbono', 1),
     (3, N'Organización',  N'Empresa o institución que mide su huella de carbono', 1);
    SET IDENTITY_INSERT ROL OFF;
END
GO

/* Estos nombres los verifica el código (HDC.BLL/Comun/Permisos.cs) */
IF NOT EXISTS (SELECT 1 FROM PERMISOS)
INSERT INTO PERMISOS (NOMBRE, DESCRIPCION) VALUES
 (N'ADMIN_PANEL',              N'Acceso al panel de administración'),
 (N'GESTIONAR_FUENTES',        N'Alta, baja y modificación de fuentes de emisión'),
 (N'GESTIONAR_USUARIOS',       N'Ver usuarios registrados y su información'),
 (N'GESTIONAR_ROLES',          N'Gestionar tipos de usuario y permisos'),
 (N'GESTIONAR_CATALOGOS',      N'Gestionar tipos de emisión, unidades de medida y alcances'),
 (N'REPORTES_GLOBALES',        N'Generar reportes con métricas de todos los usuarios'),
 (N'VER_DASHBOARD',            N'Ver el dashboard de emisiones propias'),
 (N'GESTIONAR_DOMICILIOS',     N'Alta, baja y modificación de domicilios propios'),
 (N'GESTIONAR_EMISIONES',      N'Crear, editar y eliminar registros de emisiones'),
 (N'GESTIONAR_COLABORACIONES', N'Invitar colaboradores a los domicilios propios'),
 (N'COLABORAR',                N'Cargar emisiones en domicilios donde fue invitado'),
 (N'GENERAR_REPORTES',         N'Generar y descargar reportes de emisiones');
GO

/* Asignación de permisos a los roles del sistema */
IF NOT EXISTS (SELECT 1 FROM ROL_PERMISO)
BEGIN
    -- Administrador
    INSERT INTO ROL_PERMISO (ID_ROL, ID_PERMISO)
    SELECT 1, ID_PERMISO FROM PERMISOS
    WHERE NOMBRE IN (N'ADMIN_PANEL', N'GESTIONAR_FUENTES', N'GESTIONAR_USUARIOS', N'GESTIONAR_ROLES',
                     N'GESTIONAR_CATALOGOS', N'REPORTES_GLOBALES', N'GENERAR_REPORTES');

    -- Individuo y Organización
    INSERT INTO ROL_PERMISO (ID_ROL, ID_PERMISO)
    SELECT R.ID_ROL, P.ID_PERMISO
    FROM ROL R CROSS JOIN PERMISOS P
    WHERE R.ID_ROL IN (2, 3)
      AND P.NOMBRE IN (N'VER_DASHBOARD', N'GESTIONAR_DOMICILIOS', N'GESTIONAR_EMISIONES',
                       N'GESTIONAR_COLABORACIONES', N'COLABORAR', N'GENERAR_REPORTES');
END
GO

IF NOT EXISTS (SELECT 1 FROM ALCANCE)
BEGIN
    SET IDENTITY_INSERT ALCANCE ON;
    INSERT INTO ALCANCE (ID_ALCANCE, NOMBRE, DESCRIPCION) VALUES
     (1, N'Alcance 1', N'Emisiones directas de fuentes propias o controladas'),
     (2, N'Alcance 2', N'Emisiones indirectas por la energía adquirida'),
     (3, N'Alcance 3', N'Otras emisiones indirectas (transporte de terceros, residuos, agua, viajes)');
    SET IDENTITY_INSERT ALCANCE OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM UNIDAD_MEDIDA)
BEGIN
    SET IDENTITY_INSERT UNIDAD_MEDIDA ON;
    INSERT INTO UNIDAD_MEDIDA (ID_UNIDAD_MEDIDA, NOMBRE) VALUES
     (1, N'm3'), (2, N'kg'), (3, N'L'), (4, N'kWh'), (5, N'km');
    SET IDENTITY_INSERT UNIDAD_MEDIDA OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM TIPO_EMISION)
BEGIN
    SET IDENTITY_INSERT TIPO_EMISION ON;
    INSERT INTO TIPO_EMISION (ID_TIPO_EMISION, ID_ALCANCE, NOMBRE) VALUES
     (1, 1, N'Combustión estacionaria'),
     (2, 1, N'Combustión móvil propia'),
     (3, 1, N'Emisiones fugitivas'),
     (4, 2, N'Electricidad adquirida'),
     (5, 3, N'Transporte de colaboradores'),
     (6, 3, N'Viajes'),
     (7, 3, N'Residuos'),
     (8, 3, N'Agua');
    SET IDENTITY_INSERT TIPO_EMISION OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM FUENTE_EMISION)
INSERT INTO FUENTE_EMISION (ID_UNIDAD_MEDIDA, ID_TIPO_EMISION, NOMBRE, VALOR_FACTOR_EMISION) VALUES
 (1, 1, N'Gas natural de red',            1.950000),
 (2, 1, N'GLP (garrafa)',                 2.985000),
 (3, 1, N'Gasoil para grupo electrógeno', 2.680000),
 (3, 2, N'Nafta - vehículo propio',       2.270000),
 (3, 2, N'Gasoil - vehículo propio',      2.680000),
 (1, 2, N'GNC - vehículo propio',         1.950000),
 (2, 3, N'Recarga de refrigerante R-410A', 2088.000000),
 (4, 4, N'Energía eléctrica de red',      0.400000),
 (5, 5, N'Auto particular (nafta)',       0.180000),
 (5, 5, N'Motocicleta',                   0.100000),
 (5, 5, N'Colectivo urbano',              0.090000),
 (5, 5, N'Tren',                          0.040000),
 (5, 6, N'Avión - vuelo de cabotaje',     0.150000),
 (2, 7, N'Residuos sólidos urbanos',      0.580000),
 (1, 8, N'Agua de red',                   0.344000);
GO


/* =====================================================================
   3. ÍNDICES DE RENDIMIENTO
   ---------------------------------------------------------------------
   SQL Server NO crea índices automáticamente para las claves foráneas
   (solo para PRIMARY KEY y UNIQUE). Sin estos índices, cada JOIN o WHERE
   por esas columnas recorre la tabla completa ("table scan").

   INCLUDE (...) agrega columnas a la hoja del índice: si una consulta solo
   necesita esas columnas, SQL Server la resuelve leyendo el índice, sin ir
   a la tabla ("índice de cobertura").
   ===================================================================== */

/* Registros de un domicilio por período (listados y validación de período repetido) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_REGISTROS_DOMICILIO_PERIODO')
    CREATE INDEX IX_REGISTROS_DOMICILIO_PERIODO ON REGISTROS (ID_DOMICILIO, PERIODO);
GO

/* Detalles de un registro: se usan al abrir un registro, al listar y en todas las sumas del Dashboard/reportes */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DETALLE_REGISTRO_REGISTRO')
    CREATE INDEX IX_DETALLE_REGISTRO_REGISTRO ON DETALLE_REGISTRO (ID_REGISTRO)
        INCLUDE (ID_FUENTE_EMISION, CONSUMO, KG_CO2);
GO

/* Agrupar / filtrar consumos por fuente de emisión (reportes por fuente, tipo o alcance) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DETALLE_REGISTRO_FUENTE')
    CREATE INDEX IX_DETALLE_REGISTRO_FUENTE ON DETALLE_REGISTRO (ID_FUENTE_EMISION)
        INCLUDE (ID_REGISTRO, CONSUMO, KG_CO2);
GO

/* Domicilios de un usuario: "Mis domicilios", Dashboard y la visibilidad de "Mis emisiones" */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DOMICILIO_USUARIO')
    CREATE INDEX IX_DOMICILIO_USUARIO ON DOMICILIO (ID_USUARIO)
        INCLUDE (FECHA_BAJA, TOTAL_KG_CO2);
GO

/* Filtros geográficos de los reportes (ciudad → distrito → provincia) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DOMICILIO_CIUDAD')
    CREATE INDEX IX_DOMICILIO_CIUDAD ON DOMICILIO (ID_CIUDAD);
GO

/* Registros cargados por un colaborador (también aparecen en su "Mis emisiones") */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_REGISTROS_USUARIO_CARGA')
    CREATE INDEX IX_REGISTROS_USUARIO_CARGA ON REGISTROS (ID_USUARIO_CARGA)
        INCLUDE (ID_DOMICILIO, PERIODO, TOTAL_KG_CO2);
GO

/* Colaboraciones de un domicilio por estado (validar quién puede cargar, conteo de colaboradores activos) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_COLABORACION_DOMICILIO_ESTADO')
    CREATE INDEX IX_COLABORACION_DOMICILIO_ESTADO ON COLABORACION (ID_DOMICILIO, ESTADO);
GO

/* Colaboraciones recibidas por un usuario */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_COLABORACION_COLABORADOR')
    CREATE INDEX IX_COLABORACION_COLABORADOR ON COLABORACION (ID_USUARIO_COLABORADOR);
GO

/* Usuarios por tipo (filtro del listado del admin y reportes globales por tipo de usuario) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_USUARIO_ROL')
    CREATE INDEX IX_USUARIO_ROL ON USUARIO (ID_ROL);
GO

/* Reportes guardados de un usuario */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_REPORTE_USUARIO')
    CREATE INDEX IX_REPORTE_USUARIO ON REPORTE (ID_USUARIO);
GO
