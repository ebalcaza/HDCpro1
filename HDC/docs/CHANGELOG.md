# Historial de cambios (CHANGELOG) — Huella de Carbono (HDC)

Registro de todos los cambios del sistema, **del más nuevo al más viejo**. Cada entrada indica qué se
cambió, por qué y qué archivos se tocaron.

> **Cómo agregar una entrada:** copiá la plantilla del final, pegala arriba de la entrada más reciente y
> completala. No crees archivos sueltos de cambios: todo va acá (ver
> [Guía del desarrollador § 11](GUIA_DESARROLLADOR.md#11-cómo-mantener-esta-documentación)).

---

## [2.1] — 31/10/2026 — Sección "Fuentes de emisión" para usuarios y corrección de catálogos del admin

### 2.1.1 Nueva sección "Fuentes de emisión" (Mi huella)

**Por qué:** el usuario solo veía las fuentes como una lista desplegable al cargar un registro, sin saber
qué era cada una, en qué unidad cargarla ni dónde encontrar el dato. Se pidió una sección para ver todas
las fuentes disponibles con más información.

**Qué se hizo:**
- Cada fuente tiene ahora una **descripción** (qué es y dónde encontrar el dato), editable por el
  administrador. Las 15 fuentes iniciales traen un texto de ejemplo.
- Nueva pantalla `/fuentes`: fórmula del cálculo, explicación de los alcances, fuentes agrupadas por
  alcance y tipo con unidad, factor y descripción; buscador sin distinguir tildes ni mayúsculas, filtro por
  alcance, **calculadora rápida** por fuente y botón **Cargar**, que abre el formulario de registro con la
  fuente (y el consumo) precargados.
- No hizo falta un endpoint nuevo: usa `GET /api/catalogos/fuentes`, `alcances` y `tipos-emision`
  (lectura permitida a cualquier usuario autenticado y cacheada).

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Columna `fuente_emision.descripcion VARCHAR(500)`; `ALTER TABLE ... ADD COLUMN IF NOT EXISTS` para bases creadas con la v2.0; `UPDATE` que completa la descripción de las fuentes iniciales **solo si nunca fueron editadas** (`fecha_modificacion IS NULL`) y no tienen texto, para no pisar cambios del administrador |
| `backend/HDC.Entity/Entities/Catalogos.cs` | Propiedad `FuenteEmision.Descripcion` |
| `backend/HDC.Entity/Dtos/CatalogoDtos.cs` | `FuenteEmisionDto.Descripcion` y `FuenteEmisionRequest.Descripcion` (máx. 500) |
| `backend/HDC.DAL/HdcDbContext.cs` | Mapeo de la columna `DESCRIPCION` |
| `backend/HDC.BLL/Comun/Mapeos.cs`, `Servicios/CatalogoServicios.cs` | Se incluye la descripción en el DTO y se guarda al crear o editar |
| `frontend/src/pages/usuario/FuentesEmision.jsx` | **Nueva** pantalla |
| `frontend/src/App.jsx`, `components/layout/Layout.jsx` | Ruta `/fuentes` y opción de menú (permisos `GESTIONAR_EMISIONES` o `COLABORAR`) |
| `frontend/src/pages/usuario/RegistroForm.jsx` | Acepta `?fuente=ID&consumo=N` para precargar la primera fila; si la fuente no existe se ignora |
| `frontend/src/pages/admin/Fuentes.jsx` | Campo "Descripción para los usuarios" en el formulario |

Documentos actualizados: MANUAL_USUARIO (§ 3 menú, § 5 "Antes de cargar", § 12 anexo admin),
DOCUMENTACION_TECNICA (§ 3.2 frontend, § 4 modelo de datos).

### 2.1.2 Corrección: listas mezcladas en Alcances / Unidades de medida / Tipos de emisión

**Problema reportado:** en el panel del administrador, al pasar de "Alcances" a "Unidades de medida" o
"Tipos de emisión", la tabla mostraba ítems de distintas secciones mezclados.

**Causa:** las tres rutas usan el mismo componente `Catalogos` y solo cambia la propiedad `tipo`. Como
ocupa la misma posición en el árbol, React **no lo vuelve a crear al navegar, lo reutiliza**:
1. hasta que llegan los datos nuevos, la tabla muestra la lista anterior con las columnas de la sección nueva;
2. las filas se identifican con el campo id de la sección nueva (por ejemplo `idUnidadMedida`), que las
   filas viejas no tienen: React se queda sin claves válidas y mezcla filas al actualizar la tabla;
3. la tabla conserva la búsqueda, la página y el orden de la sección anterior.

**Solución:** cada ruta le pasa una `key` distinta al componente (`key="alcances"`, `"unidades"`,
`"tipos-emision"`). Con una key distinta React desmonta el componente anterior y crea uno nuevo, con su
estado limpio. Lo mismo se aplicó a `Reportes` (`/reportes` y `/admin/reportes` usan el mismo componente).

| Archivo | Cambio |
|---|---|
| `frontend/src/App.jsx` | `key` en las rutas de `Catalogos` y `Reportes`, con comentario explicativo |

### 2.1.3 Verificación realizada

Sobre la base de la v2.0 ya existente (con datos), después de `docker compose up -d --build api frontend`:

| Prueba | Resultado |
|---|---|
| `dotnet build` y `npm run build` | Compilan sin errores |
| Arranque: el script agrega la columna `descripcion` a la base existente | OK: 15 de 15 fuentes con descripción |
| Un usuario (Juan) lista las fuentes | Recibe las 15 con su descripción |
| El admin edita la descripción de una fuente | Se guarda |
| Descripción de 501 caracteres | 400 "La descripción admite hasta 500 caracteres" |
| Reiniciar la API (el script se ejecuta de nuevo) | La descripción editada por el admin **no** se pisa |
| Ruta `/fuentes` servida por nginx, pantalla incluida en la versión publicada | OK |
| `key` de las rutas `Catalogos` y `Reportes` en la versión publicada | Presentes |

Pendiente de verificar a mano en el navegador: la vista de la sección "Fuentes de emisión" (diseño,
calculadora, botón Cargar) y la navegación Alcances → Unidades → Tipos de emisión en el panel del admin.

---

## [2.0] — 31/10/2026 — PostgreSQL, recuperación de contraseña, datos de prueba y documentación

Pedido original: `textos/fix4.txt`. Se hicieron cuatro cambios grandes, en este orden.

### 2.0.1 Migración de la base de datos de SQL Server a PostgreSQL

**Por qué:** SQL Server en Docker necesita ~2 GB de RAM y en equipos de 4 GB tardaba hasta 10 minutos en
arrancar o quedaba *unhealthy*. PostgreSQL usa ~50–100 MB, arranca en segundos, es libre y gratuito sin
límites de edición, y es un motor muy usado en producción. Se empezó con una **base nueva** (los datos de
SQL Server no se migraron; se reemplazan por datos de prueba, ver 2.0.3).

**Qué se hizo:**

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/HDC.DAL.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` → `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 |
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Reescrito para PostgreSQL manteniendo las mismas tablas, datos iniciales e índices (detalle abajo). Se agregó la tabla `recuperacion_contrasena` (ver 2.0.2) e índices para `distrito.id_provincia` y `ciudad.id_distrito` |
| `backend/HDC.DAL/InicializadorBaseDatos.cs` | Reescrito con Npgsql: se conecta a la base `postgres` para crear la base si no existe (consulta `pg_database`, porque PostgreSQL no tiene `CREATE DATABASE IF NOT EXISTS`) y ejecuta el script en una sola transacción. Ya no divide el script en lotes `GO` |
| `backend/HDC.DAL/HdcDbContext.cs` | Tipos de fecha `smalldatetime` → `timestamp(0) without time zone` y `datetime` → `timestamp(3) without time zone`. Nuevo método `AMinusculas` que pasa a minúscula los nombres de tablas y columnas (ver "nombres" abajo). Mapeo de `RecuperacionContrasena` |
| `backend/HDC.BLL/DependencyInjection.cs` | `UseSqlServer` → `UseNpgsql` (con reintentos ante cortes de conexión) |
| `backend/HDC.API/Infraestructura/ManejoErroresMiddleware.cs` | Los errores de SQL Server 547 (FK) y 2627/2601 (UNIQUE) pasan a los códigos de PostgreSQL `23503` y `23505` |
| `backend/HDC.DAL/Repositorios/ExtensionesConsulta.cs` | Nuevo `PatronContiene`: arma `%texto%` escapando `%`, `_` y `\` |
| `backend/HDC.DAL/Repositorios/EmisionRepositorios.cs`, `SeguridadRepositorios.cs` | Buscadores de "Mis emisiones" y "Usuarios": `Contains` → `EF.Functions.ILike` |
| `backend/HDC.DAL/Repositorios/CatalogoRepositorios.cs`, `SeguridadRepositorios.cs` | Detección de nombres duplicados (alcances, unidades, roles, permisos) sin distinguir mayúsculas (`ToLower()`) |
| `docker-compose.yml` | Servicio `db`: imagen `postgres:17-alpine`, volumen `pgdata`, healthcheck con `pg_isready` (5 s en vez de hasta 20 min), puerto publicado solo en `127.0.0.1`. Cadena de conexión de la API en formato Npgsql |
| `.env.example`, `.env` | `DB_SA_PASSWORD` → `DB_USUARIO` + `DB_CONTRASENA`; `DB_NAME` = `huellacarbono`; `DB_PORT` = `5433` |

**Traducción del script SQL Server → PostgreSQL:**

| SQL Server | PostgreSQL | Nota |
|---|---|---|
| `IF OBJECT_ID(N'X', N'U') IS NULL CREATE TABLE X` | `CREATE TABLE IF NOT EXISTS x` | |
| `BIGINT IDENTITY(1,1)` | `BIGINT GENERATED BY DEFAULT AS IDENTITY` | "BY DEFAULT" permite insertar ids fijos en los datos iniciales |
| `SET IDENTITY_INSERT X ON/OFF` | `setval(pg_get_serial_sequence(...), MAX(id))` | Después de insertar ids fijos se adelanta la secuencia |
| `NVARCHAR(n)` / `NVARCHAR(MAX)` | `VARCHAR(n)` / `TEXT` | En PostgreSQL todo texto es Unicode |
| `BIT` | `BOOLEAN` | |
| `DECIMAL(p,s)` | `NUMERIC(p,s)` | |
| `SMALLDATETIME` / `DATETIME` | `TIMESTAMP(0)` / `TIMESTAMP(3)` | Sin zona horaria, hora de Argentina |
| `DEFAULT (GETDATE())` | `DEFAULT LOCALTIMESTAMP(n)` | |
| `CHECK (DAY(PERIODO) = 1)` | `CHECK (EXTRACT(DAY FROM periodo) = 1)` | |
| `IF NOT EXISTS (SELECT 1 FROM X) INSERT ... VALUES` | `INSERT ... SELECT ... FROM (VALUES ...) WHERE NOT EXISTS (...)` | |
| `IF NOT EXISTS (SELECT 1 FROM sys.indexes ...) CREATE INDEX` | `CREATE INDEX IF NOT EXISTS` | `INCLUDE (...)` funciona igual |
| Lotes separados por `GO` | Sentencias separadas por `;` | |
| `DROP TABLE VEHICULO` (limpieza de bases viejas) | Eliminado | La base es nueva |

**Nombres en minúscula:** PostgreSQL convierte a minúscula todo nombre sin comillas, pero EF Core siempre
usa comillas (`"USUARIO"` ≠ `usuario`). El script crea todo en minúscula y `HdcDbContext.AMinusculas`
convierte los nombres del mapeo, que siguen escritos en MAYÚSCULA para leerse como el diagrama original.

**Diferencias de comportamiento resueltas:**
- En SQL Server `LIKE` y `=` no distinguían mayúsculas (intercalación por defecto); en PostgreSQL sí. Se usó
  `ILIKE` en los buscadores y `lower()` en los controles de duplicados, para que todo siga funcionando igual.
- Npgsql exige que las columnas `timestamp without time zone` reciban fechas locales; el código ya usaba
  `DateTime.Now`, así que no hubo que cambiar los servicios.

### 2.0.2 Recuperación de contraseña por email

**Por qué:** el usuario que olvida su contraseña no tenía forma de recuperarla. Se pidió un link por email
con vencimiento que lleve a un formulario para crear la contraseña nueva. Como el sistema va a producción,
se diseñó siguiendo prácticas de seguridad estándar (detalle en
[Documentación técnica § 7](DOCUMENTACION_TECNICA.md#7-recuperación-de-contraseña)).

| Archivo | Cambio |
|---|---|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Tabla `recuperacion_contrasena` (hash del token, creación, vencimiento, uso) + índice por usuario |
| `backend/HDC.Entity/Entities/Usuarios.cs` | Entidad `RecuperacionContrasena` |
| `backend/HDC.Entity/Dtos/AuthDtos.cs` | `SolicitarRecuperacionRequest` y `RestablecerContrasenaRequest` (mismas reglas de contraseña que el registro) |
| `backend/HDC.DAL/Repositorios/SeguridadRepositorios.cs` | `RecuperacionRepositorio` |
| `backend/HDC.BLL/Servicios/RecuperacionServicio.cs` | **Nuevo.** Genera el token (256 bits), guarda su SHA-256, valida y cambia la contraseña; plantillas de los emails |
| `backend/HDC.BLL/Servicios/EmailServicio.cs` | **Nuevo.** `ColaEmails` (cola en memoria) + `EnvioEmailsServicio` (envío SMTP en segundo plano con MailKit, 3 reintentos) |
| `backend/HDC.BLL/HDC.BLL.csproj` | Paquetes `MailKit` y `Microsoft.Extensions.Hosting.Abstractions` |
| `backend/HDC.BLL/DependencyInjection.cs` | Registro de repositorio, servicios, cola y servicio en segundo plano |
| `backend/HDC.API/Controllers/AuthController.cs` | `POST /api/auth/recuperar` y `POST /api/auth/restablecer` |
| `backend/HDC.API/Infraestructura/LimitesPedidos.cs` | **Nuevo.** Límite de 5 pedidos cada 15 minutos por IP (HTTP 429) y lectura de la IP real detrás de nginx (`X-Forwarded-For`) |
| `backend/HDC.API/Program.cs` | `AgregarLimitesPedidos`, `UseForwardedHeaders`, `UseRateLimiter` |
| `backend/HDC.API/appsettings.json` | Secciones `Email` y `Recuperacion` |
| `docker-compose.yml`, `.env.example` | Servicio `mailpit` (correo de prueba en http://localhost:8025) y variables `SMTP_*`, `EMAIL_*`, `URL_PUBLICA`, `RECUPERACION_MINUTOS` |
| `frontend/src/api/servicios.js` | `authApi.solicitarRecuperacion` y `authApi.restablecerContrasena` |
| `frontend/src/pages/publicas/OlvideContrasena.jsx` | **Nueva** pantalla: pedir el link |
| `frontend/src/pages/publicas/RestablecerContrasena.jsx` | **Nueva** pantalla: crear la nueva contraseña desde el link |
| `frontend/src/App.jsx` | Rutas públicas `/olvide-contrasena` y `/restablecer-contrasena` |
| `frontend/src/pages/publicas/Login.jsx` | Link "¿Olvidaste tu contraseña?" |

Proveedor de email: el código usa SMTP estándar, así que el proveedor se elige solo con configuración.
Para producción se recomienda un servicio transaccional (Brevo, Amazon SES, Mailgun…) o el SMTP
institucional, con remitente de dominio propio con SPF/DKIM (ver Guía del desarrollador § 9).

### 2.0.3 Datos de prueba

**Por qué:** al empezar con una base nueva hacen falta usuarios y consumos para ver y probar todas las pantallas.

| Archivo | Cambio |
|---|---|
| `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs` | **Nuevo.** Crea 2 individuos y 1 organización (contraseña `Prueba2026`), un domicilio cada uno, 12 meses de consumos con variación por estación, una colaboración aceptada (Juan en la casa de María) con sus registros y una invitación pendiente (EcoSur → María). Idempotente |
| `backend/HDC.API/Program.cs` | Se ejecuta después de descargar la geografía (los domicilios necesitan una ciudad) |
| `backend/HDC.API/appsettings.json`, `docker-compose.yml`, `.env.example` | Opción `DatosPrueba:Habilitado` / variable `DATOS_PRUEBA` (falso por defecto; en el `.env` de desarrollo, verdadero) |

El administrador sigue creándose como antes, con `ADMIN_EMAIL` / `ADMIN_CONTRASENA`.
Cuentas y detalle: [Guía del desarrollador § 3](GUIA_DESARROLLADOR.md#3-usuarios-y-datos-de-prueba).

### 2.0.4 Documentación reorganizada

**Por qué:** para la entrega formal se pidió una documentación ordenada, sin muchos archivos sueltos, y
fácil de actualizar con cambios futuros.

| Antes | Ahora |
|---|---|
| `README.md` con instalación, desarrollo, arquitectura, dominio y problemas (305 líneas) | `README.md`: portada breve con inicio rápido e índice |
| `MANUAL_USUARIO.md` en la raíz | `docs/MANUAL_USUARIO.md` (+ sección "¿Olvidaste tu contraseña?" y preguntas frecuentes) |
| Instalación y desarrollo dentro del README | `docs/GUIA_DESARROLLADOR.md` (Docker, desarrollo local, variables, datos de prueba, Mailpit, cambios en la base, convenciones, producción, problemas frecuentes) |
| Arquitectura y dominio dentro del README | `docs/DOCUMENTACION_TECNICA.md` (tecnologías, arquitectura, modelo de datos, API, seguridad, recuperación, rendimiento, decisiones) |
| Notas `textos/CAMBIOS_*.md` sueltas | `docs/CHANGELOG.md` (este archivo), con un resumen de cada cambio anterior |

La carpeta `textos/` (consignas y notas de trabajo) no se modificó; su organización queda a cargo del autor.

### 2.0.5 Verificación realizada

Con `docker compose up -d --build` sobre una base vacía (las pruebas se hicieron contra la API a través de
nginx, como lo usa el navegador):

| Prueba | Resultado |
|---|---|
| `dotnet build HDC.sln` y `npm run build` | Compilan sin errores |
| Arranque: script SQL, administrador, georef (24 provincias / 529 distritos / 4.037 ciudades), datos de prueba | OK, sin errores en el log |
| Login de las 4 cuentas (admin + 3 de prueba) | OK |
| Dashboard, listado de registros, colaboraciones recibidas | OK, con los datos de prueba |
| Buscador sin distinguir mayúsculas (`COLECTIVO`, `GÓMEZ`) | OK (12 registros / 1 usuario) |
| Admin: usuarios, resumen, reporte global, fuentes | OK |
| Alta de unidad duplicada con otras mayúsculas (`KWH` vs `kWh`) | 409 "Ya existe una unidad con ese nombre" |
| Recuperación: email existente e inexistente | Misma respuesta; solo el existente recibe email en Mailpit |
| Token guardado en la base | Se guarda el hash (distinto del token); validez 30 min |
| Restablecer con el link | OK; la contraseña nueva funciona y la vieja da 401; llega email de confirmación |
| Reusar el mismo link | 400 "El link no es válido o ya venció" |
| Límite de pedidos | El 6.º pedido en 15 minutos responde 429 |
| Ruta `/restablecer-contrasena` servida por nginx | 200 |

No se probó con un proveedor SMTP real (solo Mailpit): al configurarlo para producción, repetir la prueba de
recuperación.

---

## [1.3] — 10/2026 — Corrección del valor de 1 ppm

- El proyecto usaba **1 ppm = 17.600 millones de t CO₂**; el valor correcto es **≈ 7.800 millones**
  (1 ppm ≈ 2,13 Gt C × 3,664). Con el valor viejo los ppm mostrados quedaban 2,26 veces más chicos.
- Archivos: `frontend/src/utils/formato.js` (`TONELADAS_POR_PPM` y nueva `EQUIVALENCIA_PPM`, único lugar
  donde se define), `Dashboard.jsx` y `utils/pdf.js` usan la constante; documentación corregida.
- Nota original: `textos/CAMBIOS_valor_ppm.md`.

## [1.2] — 10/2026 — Se quita "vehículos" y se unifica el script de base de datos

- La sección de vehículos ya no existía en la aplicación: se eliminó la tabla `VEHICULO` del script (y se
  borraba de bases existentes al arrancar). Nota: `textos/CAMBIOS_quitar_vehiculos.md`.
- Los scripts `01_esquema.sql`, `02_datos_iniciales.sql` y `03_indices.sql` se unificaron en un único
  script idempotente `HDC_base_datos.sql`, que la API ejecuta en cada arranque.
  Nota: `textos/CAMBIOS_script_unico_bd.md`.
- Healthcheck del contenedor de SQL Server más tolerante (hasta ~20 min) para equipos con poca RAM.
  Nota: `textos/CAMBIOS_healthcheck_db.md`. *(Reemplazado en 2.0 por PostgreSQL.)*

## [1.1] — 21/10/2026 — Mejoras de rendimiento

1. Paginado en el servidor en "Mis emisiones" y "Usuarios" (`{ items, total }`).
2. Totales del Dashboard calculados con SQL (`GROUP BY`) en lugar de en memoria.
3. Índices sobre las claves foráneas usadas en filtros, con columnas `INCLUDE`.
4. Cache en memoria de catálogos y geografía (`CacheCatalogos`).
5. Carga diferida del generador de PDF (`utils/pdfDiferido.js`).
6. Espera en el buscador (debounce), compresión Brotli/gzip y proyección liviana del listado de usuarios.

Nota original con el detalle de cada punto: `textos/CAMBIOS_rendimiento.md`.

## [1.0] — 09/10/2026 — Primera versión funcionando

Sistema completo: registro de individuos y organizaciones, domicilios con geografía de georef, carga de
consumos mensuales, dashboard, reportes PDF, colaboraciones y panel de administración (fuentes, usuarios,
roles, permisos y catálogos). Backend .NET 9 en 4 capas con SQL Server; frontend React; Docker Compose.

Cambios respecto del esquema de base de datos original de la consigna:

| Cambio | Motivo |
|---|---|
| PK autonuméricas (salvo provincia/distrito/ciudad, que usan el id de georef) | IDs generados por la base |
| `CIUDAD.NOMBRE` BIGINT → texto | Era un error de tipo |
| `FECHA_BAJA` admite NULL | Activo = sin fecha de baja |
| `REGISTROS` pasa a cabecera (domicilio + período + quién cargó) con `DETALLE_REGISTRO` (fuente, consumo, factor, kg CO₂) | Un registro mensual tiene varios consumos; el factor se congela |
| `ROL_PERMISO` (N:M) | Un permiso puede pertenecer a varios tipos de usuario |
| `INDIVIDUO` + DNI y género · `ORGANIZACION` + descripción y cantidad de miembros · `DOMICILIO` + block, manzana y teléfono | Datos del glosario |
| `COLABORACION` + `COLABORACION_TIPO_EMISION` (reemplaza `COLABORA`) | Colaboradores por tipo de emisión y período |
| `REPORTE` | Guardar combinaciones de filtros |
| Unidades `kWh` y `km` | Necesarias para electricidad y transporte |

---

## Plantilla para nuevas entradas

```markdown
## [X.Y] — DD/MM/AAAA — Título corto

**Por qué:** problema o pedido que motivó el cambio.

| Archivo | Cambio |
|---|---|
| `ruta/al/archivo` | Qué se cambió |

Documentos actualizados: (MANUAL_USUARIO / GUIA_DESARROLLADOR / DOCUMENTACION_TECNICA, sección).
```

Versionado: se sube el número mayor (2.0 → 3.0) con cambios que obligan a reinstalar o migrar datos, y el
menor (2.0 → 2.1) con funcionalidades o correcciones compatibles.
