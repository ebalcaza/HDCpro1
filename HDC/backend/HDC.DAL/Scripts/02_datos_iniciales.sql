/* =====================================================================
   HDC - Datos iniciales (catálogos, roles y permisos)
   Provincia / Distrito / Ciudad se cargan desde la API georef al iniciar.
   El usuario administrador se crea desde la API (contraseña con hash BCrypt).
   Los factores de emisión son valores de referencia: el administrador
   debe revisarlos y ajustarlos según la fuente oficial que utilice.
   ===================================================================== */

SET IDENTITY_INSERT ROL ON;
INSERT INTO ROL (ID_ROL, NOMBRE, DESCRIPCION, ES_SISTEMA) VALUES
 (1, N'Administrador', N'Gestiona cuentas, factores de emisión y parametrización del sistema', 1),
 (2, N'Individuo',     N'Persona que mide su huella de carbono', 1),
 (3, N'Organización',  N'Empresa o institución que mide su huella de carbono', 1);
SET IDENTITY_INSERT ROL OFF;
GO

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

INSERT INTO ROL_PERMISO (ID_ROL, ID_PERMISO)
SELECT 1, ID_PERMISO FROM PERMISOS
WHERE NOMBRE IN (N'ADMIN_PANEL', N'GESTIONAR_FUENTES', N'GESTIONAR_USUARIOS', N'GESTIONAR_ROLES',
                 N'GESTIONAR_CATALOGOS', N'REPORTES_GLOBALES', N'GENERAR_REPORTES');

INSERT INTO ROL_PERMISO (ID_ROL, ID_PERMISO)
SELECT R.ID_ROL, P.ID_PERMISO
FROM ROL R CROSS JOIN PERMISOS P
WHERE R.ID_ROL IN (2, 3)
  AND P.NOMBRE IN (N'VER_DASHBOARD', N'GESTIONAR_DOMICILIOS', N'GESTIONAR_EMISIONES',
                   N'GESTIONAR_COLABORACIONES', N'COLABORAR', N'GENERAR_REPORTES');
GO

SET IDENTITY_INSERT ALCANCE ON;
INSERT INTO ALCANCE (ID_ALCANCE, NOMBRE, DESCRIPCION) VALUES
 (1, N'Alcance 1', N'Emisiones directas de fuentes propias o controladas'),
 (2, N'Alcance 2', N'Emisiones indirectas por la energía adquirida'),
 (3, N'Alcance 3', N'Otras emisiones indirectas (transporte de terceros, residuos, agua, viajes)');
SET IDENTITY_INSERT ALCANCE OFF;
GO

SET IDENTITY_INSERT UNIDAD_MEDIDA ON;
INSERT INTO UNIDAD_MEDIDA (ID_UNIDAD_MEDIDA, NOMBRE) VALUES
 (1, N'm3'), (2, N'kg'), (3, N'L'), (4, N'kWh'), (5, N'km');
SET IDENTITY_INSERT UNIDAD_MEDIDA OFF;
GO

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
GO

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
