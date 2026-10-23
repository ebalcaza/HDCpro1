# Huella de Carbono (HDC)

Aplicación web para que personas y organizaciones calculen su **huella de carbono** a partir de sus consumos
mensuales (gas, electricidad, combustible, transporte, residuos, agua...).

> ¿Sos usuario final y solo querés usar la aplicación? Leé el [Manual de usuario](MANUAL_USUARIO.md).

## Tecnologías

| Parte          | Tecnología                                                       | Carpeta     |
|----------------|------------------------------------------------------------------|-------------|
| Frontend       | React 19 + Vite 6 + Bootstrap 5 + Chart.js + jsPDF               | `frontend/` |
| Backend (API)  | .NET 9 / C#, 4 capas, EF Core, JWT                               | `backend/`  |
| Base de datos  | SQL Server (Express 2025 en contenedor, o LocalDB / instancia local) | —       |
| Orquestación   | Docker Compose (db + api + frontend servido por nginx)           | raíz        |

## Estructura

```
HDC/
├─ docker-compose.yml         db (SQL Server) + api (.NET) + frontend (nginx)
├─ .env.example               variables de entorno (copiar a .env)
├─ README.md                  este archivo
├─ MANUAL_USUARIO.md          manual para el usuario final
├─ textos/                    notas de cada cambio hecho durante el desarrollo
├─ backend/
│  ├─ HDC.sln
│  ├─ HDC.Entity/             Entidades del ORM, DTOs y reglas de validación
│  ├─ HDC.DAL/                DbContext, repositorios y Scripts/HDC_base_datos.sql
│  ├─ HDC.BLL/                Servicios (lógica de negocio), permisos, JWT, cache, semillas
│  └─ HDC.API/                Controladores REST, autenticación, manejo de errores
└─ frontend/
   ├─ src/api/                Cliente axios y llamadas a la API
   ├─ src/pages/              Pantallas (publicas/, usuario/, admin/)
   ├─ src/components/         Componentes reutilizables (tablas, formularios, gráficos)
   ├─ src/utils/              Formatos, colores y generación de PDF (carga diferida)
   └─ nginx.conf              Sirve la SPA y hace de proxy de /api hacia la API
```

---

## 1. Usarlo en una PC (Docker)

Es la forma más simple: no hace falta instalar .NET, Node ni SQL Server.

### Requisitos
- **Docker Desktop** (Windows/Mac) o Docker Engine + Compose (Linux).
- **Al menos 2 GB de RAM libres para Docker** (SQL Server los necesita). Ver [Equipos con poca memoria](#equipos-con-poca-memoria).
- Conexión a internet en el primer arranque (para descargar las imágenes y la geografía argentina).

### Pasos
1. Abrí una terminal en la carpeta `HDC/`.
2. Creá el archivo de configuración a partir del ejemplo (si no existe ya un `.env`):
   ```bash
   cp .env.example .env        # en PowerShell: Copy-Item .env.example .env
   ```
3. Editá `.env` y cambiá al menos las contraseñas y la clave JWT:

   | Variable             | Para qué sirve                                                                 |
   |----------------------|--------------------------------------------------------------------------------|
   | `DB_SA_PASSWORD`     | Contraseña del usuario `sa` de SQL Server. 8+ caracteres con mayúsculas, minúsculas, números y símbolos. **Sin `;`** |
   | `DB_NAME`            | Nombre de la base (por defecto `HuellaCarbono`)                                |
   | `DB_PORT`            | Puerto donde se publica SQL Server en tu PC (por defecto `14330`)              |
   | `JWT_CLAVE`          | Clave para firmar las sesiones. **Mínimo 32 caracteres**, o la API no arranca  |
   | `JWT_EXPIRA_MINUTOS` | Duración de la sesión (por defecto 120)                                        |
   | `ADMIN_EMAIL`        | Email del administrador inicial                                                |
   | `ADMIN_CONTRASENA`   | Contraseña del administrador inicial (se usa solo la primera vez)              |
   | `API_PORT`           | Puerto de la API en tu PC (por defecto `5000`)                                 |
   | `FRONTEND_PORT`      | Puerto de la web en tu PC (el `.env` actual usa `8081`)                        |

4. Levantá todo:
   ```bash
   docker compose up -d --build
   ```
5. Esperá a que los tres contenedores estén en marcha (`docker compose ps`). La primera vez puede tardar
   varios minutos: se descargan las imágenes y SQL Server tiene que terminar de iniciar.
6. Abrí en el navegador:
   - **Aplicación:** `http://localhost:<FRONTEND_PORT>` → por ejemplo http://localhost:8081
   - **Swagger (documentación de la API):** http://localhost:5000/swagger
   - **Estado de la API:** http://localhost:5000/api/salud
7. Ingresá con `ADMIN_EMAIL` / `ADMIN_CONTRASENA`, o creá una cuenta nueva desde **Registrate**.

### Qué pasa en el primer arranque
La API, al iniciar:
1. **Crea la base** (si no existe) y ejecuta el script único `backend/HDC.DAL/Scripts/HDC_base_datos.sql`
   (tablas, datos iniciales e índices). El script es **idempotente**: corre en cada arranque y solo crea lo que falte.
2. **Crea el administrador** con los datos del `.env`.
3. **Descarga en segundo plano** provincias, distritos y localidades desde la API pública **georef**
   (24 / 529 / ~4.000). Si falla (sin internet), se reintenta desde *Panel general → Cargar geografía*.

Datos iniciales que carga el script: 3 tipos de usuario (Administrador, Individuo, Organización), 12 permisos,
3 alcances, 5 unidades (m3, kg, L, kWh, km), 8 tipos de emisión y 15 fuentes con su factor de emisión.

### Comandos útiles

| Acción                                    | Comando                                 |
|-------------------------------------------|-----------------------------------------|
| Ver el estado de los contenedores         | `docker compose ps`                     |
| Ver los logs de la API                    | `docker compose logs -f api`            |
| Detener todo (los datos se conservan)     | `docker compose down`                   |
| Volver a levantar sin recompilar          | `docker compose up -d`                  |
| Recompilar después de cambiar el código   | `docker compose up -d --build`          |
| **Borrar todo, incluida la base de datos**| `docker compose down -v`                |

Los datos de SQL Server se guardan en el volumen `sqldata`, así que sobreviven a `down`/`up`.
Solo `down -v` los borra.

### Equipos con poca memoria
En una PC con ~4 GB de RAM, SQL Server en contenedor puede tardar **hasta 10 minutos** en quedar listo.
El healthcheck del servicio `db` ya tiene un margen de ~20 minutos para no marcarlo *unhealthy* antes de tiempo.

- Cerrá aplicaciones pesadas (navegador con muchas pestañas, IDE) antes de levantar los contenedores.
- Si aparece `dependency failed to start: container hdc-db-1 is unhealthy`, esperá a que `db` quede
  *healthy* (`docker compose ps`) y ejecutá `docker compose up -d` (sin `--build`).
- Alternativa: usar el SQL Server instalado en Windows (ver abajo) o trabajar sin Docker ([sección 2](#2-trabajarlo-en-desarrollo)).

### Usar el SQL Server instalado en Windows en lugar del contenedor
1. En *SQL Server Configuration Manager* habilitá **TCP/IP** y la **autenticación SQL** (modo mixto).
   Creá un login con permiso para crear bases.
2. En `docker-compose.yml` eliminá el servicio `db` (y el `depends_on` de `api`) y cambiá la cadena de conexión:
   ```
   Server=host.docker.internal,1433;Database=HuellaCarbono;User Id=<usuario>;Password=<clave>;TrustServerCertificate=True;
   ```
   `Trusted_Connection=True` (autenticación de Windows) **no funciona** desde un contenedor Linux.

---

## 2. Trabajarlo en desarrollo

En desarrollo conviene correr la API con `dotnet run` y el frontend con `npm run dev`: los cambios se ven
al instante y consume mucha menos memoria que los tres contenedores.

### Requisitos
- **.NET SDK 9** (`dotnet --version` → 9.x)
- **Node.js 22 o superior** + npm
- **SQL Server**: LocalDB (viene con Visual Studio), una instancia local, o solo el contenedor `db`
- Opcional: Visual Studio 2022 / Rider para el backend, VS Code para el frontend, SSMS para la base

### 2.1 Base de datos
Elegí una opción:

- **LocalDB** (recomendada en equipos con poca RAM):
  `Server=(localdb)\MSSQLLocalDB;Database=HuellaCarbono;Trusted_Connection=True;TrustServerCertificate=True;`
- **Solo el contenedor de SQL Server:** `docker compose up -d db` y usá
  `Server=localhost,14330;Database=HuellaCarbono;User Id=sa;Password=<DB_SA_PASSWORD>;TrustServerCertificate=True;`

No hace falta crear la base ni ejecutar scripts: la API lo hace sola al arrancar.

### 2.2 Backend (API)
La API necesita tres valores que **no** están en `appsettings.json` (por seguridad): cadena de conexión,
clave JWT y contraseña del administrador. Pasalos como variables de entorno (en .NET, `__` reemplaza a `:`).

PowerShell, desde `HDC/backend`:
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\MSSQLLocalDB;Database=HuellaCarbono;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Jwt__Clave = "una-clave-de-desarrollo-de-al-menos-32-caracteres"
$env:Admin__Contrasena = "Admin2026!"
dotnet run --project HDC.API --urls http://localhost:5000
```

Bash:
```bash
export ConnectionStrings__DefaultConnection='Server=(localdb)\MSSQLLocalDB;Database=HuellaCarbono;Trusted_Connection=True;TrustServerCertificate=True;'
export Jwt__Clave='una-clave-de-desarrollo-de-al-menos-32-caracteres'
export Admin__Contrasena='Admin2026!'
dotnet run --project HDC.API --urls http://localhost:5000
```

- Se usa el puerto **5000** porque es al que apunta el proxy de Vite. Si corrés con el perfil de
  `launchSettings.json` (puerto 5081, por ejemplo desde Visual Studio), iniciá el frontend con
  `VITE_API_URL=http://localhost:5081`.
- Swagger: http://localhost:5000/swagger
- Para compilar sin ejecutar: `dotnet build HDC.sln`

Otras opciones de `appsettings.json`: `Georef:Url`, `Cors:Origenes`, `Swagger:Habilitado`, `Jwt:ExpiraMinutos`,
`Admin:Email` (por defecto `admin@hdc.com`).

### 2.3 Frontend
```bash
cd frontend
npm install
npm run dev          # http://localhost:5173 — redirige /api a http://localhost:5000
```

| Script            | Qué hace                                         |
|-------------------|--------------------------------------------------|
| `npm run dev`     | Servidor de desarrollo con recarga en caliente   |
| `npm run build`   | Genera la versión de producción en `dist/`       |
| `npm run preview` | Sirve `dist/` localmente para probarla           |

Para apuntar a otra API: `VITE_API_URL=http://localhost:5081 npm run dev`
(PowerShell: `$env:VITE_API_URL="http://localhost:5081"; npm run dev`).

### 2.4 Arquitectura del backend
Cada petición recorre las capas en este orden:

```
HDC.API (Controllers)  →  HDC.BLL (Servicios)  →  HDC.DAL (Repositorios + EF Core)  →  SQL Server
              ↑ todas usan HDC.Entity (entidades, DTOs, validaciones)
```

- **Errores:** los servicios lanzan `NegocioException`, `ProhibidoException` o `NoEncontradoException`;
  `ManejoErroresMiddleware` los convierte en respuestas 400 / 403 / 404 con `{ mensaje, errores }`.
- **Permisos:** el JWT lleva un claim por permiso. Pantallas y endpoints se habilitan por **permiso**, no por
  nombre de rol, así el administrador puede crear tipos de usuario nuevos. Los cambios de permisos se aplican
  en el siguiente inicio de sesión.
- **Paginado en el servidor:** `GET /api/registros` y `GET /api/usuarios` devuelven `{ items, total }`
  (tamaños permitidos: 10, 20, 50, 100). El Dashboard se calcula con `GROUP BY` en SQL.
- **Cache:** catálogos y geografía se guardan en memoria (`CacheCatalogos`, 1 hora o hasta que se modifiquen).
- **PDF:** `jspdf` se descarga recién al primer click en "Descargar PDF" (`utils/pdfDiferido.js`).

### 2.5 Cambios en la base de datos
Todo el esquema está en **un único script**: `backend/HDC.DAL/Scripts/HDC_base_datos.sql`
(secciones: esquema, datos iniciales, índices). Va embebido en el ensamblado y se ejecuta en cada arranque
dentro de una transacción.

Para agregar algo, sumalo al final de la sección que corresponda **siempre con su condición**, para que el
script se pueda ejecutar muchas veces:

```sql
-- Tabla nueva
IF OBJECT_ID(N'NUEVA_TABLA', N'U') IS NULL
    CREATE TABLE NUEVA_TABLA (...);
GO

-- Columna nueva en una tabla existente
IF COL_LENGTH('DOMICILIO', 'NUEVA_COLUMNA') IS NULL
    ALTER TABLE DOMICILIO ADD NUEVA_COLUMNA NVARCHAR(50) NULL;
GO

-- Índice nuevo
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NUEVO')
    CREATE INDEX IX_NUEVO ON TABLA (COLUMNA);
GO
```

Después actualizá la entidad en `HDC.Entity/Entities` y su mapeo en `HDC.DAL/HdcDbContext.cs`.

---

## 3. Conceptos del dominio

### Cálculo
- `kg CO₂ = consumo × factor de emisión`. El factor **queda guardado** en cada detalle del registro, así que si
  el administrador cambia un factor no se altera el histórico.
- Los totales se muestran en **toneladas de CO₂**, junto con su equivalente atmosférico:
  `1 ppm = 17.600 millones de t CO₂`.
- Los factores iniciales son **valores de referencia**: revisalos con la fuente oficial que uses.

### Alcances (GHG Protocol)
| Alcance   | Qué incluye                                      | Ejemplos de fuentes                       |
|-----------|--------------------------------------------------|-------------------------------------------|
| Alcance 1 | Emisiones directas de fuentes propias            | Gas natural, GLP, nafta del auto propio   |
| Alcance 2 | Energía adquirida                                | Energía eléctrica de red                  |
| Alcance 3 | Otras indirectas                                 | Colectivo, tren, avión, residuos, agua    |

### Colaboradores
1. El titular invita por email a un usuario registrado para un **domicilio**, un **rango de períodos** y los
   **tipos de emisión** que puede cargar.
2. El invitado acepta o rechaza. Con la colaboración aceptada y vigente, carga consumos solo de esos tipos.
3. Esos registros quedan marcados con quién los cargó y **suman a la huella del domicilio del titular**.
4. Cualquiera de los dos puede finalizar la colaboración; los registros ya cargados se conservan.

### Tipos de usuario iniciales
| Rol           | Puede                                                                                   |
|---------------|-----------------------------------------------------------------------------------------|
| Administrador | Panel general, fuentes, usuarios, tipos de usuario y permisos, catálogos, reportes globales |
| Individuo     | Dashboard, domicilios, emisiones, colaboraciones, reportes                              |
| Organización  | Igual que Individuo                                                                     |

Los permisos del sistema y los 3 roles iniciales están protegidos contra borrado.

### Cambios respecto del esquema original
| Cambio | Motivo |
|---|---|
| PK con `IDENTITY` (salvo PROVINCIA/DISTRITO/CIUDAD, que usan el id de georef) | IDs generados por la base |
| `CIUDAD.NOMBRE` BIGINT → NVARCHAR | Era un error de tipo |
| `FECHA_BAJA` admite NULL | Activo = sin fecha de baja |
| `REGISTROS` pasa a cabecera (domicilio + período + quién cargó) con `DETALLE_REGISTRO` (fuente, consumo, factor, kg CO₂) | Un registro mensual tiene varios consumos; el factor se congela |
| `ROL_PERMISO` (N:M) | Un permiso puede pertenecer a varios tipos de usuario |
| `INDIVIDUO` + DNI, género · `ORGANIZACION` + descripción, cantidad de miembros · `DOMICILIO` + block, manzana, teléfono · `VEHICULO` | Datos del glosario |
| `COLABORACION` + `COLABORACION_TIPO_EMISION` (reemplaza `COLABORA`) | Ver colaboradores |
| `REPORTE` | Guardar combinaciones de filtros |
| Unidades `kWh` y `km` | Necesarias para electricidad y transporte |

---

## 4. Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| `container hdc-db-1 is unhealthy` | SQL Server tardó demasiado en arrancar (poca RAM). Esperá a que `db` quede *healthy* y ejecutá `docker compose up -d`. |
| La API se cierra con `Jwt:Clave debe tener al menos 32 caracteres` | Alargá `JWT_CLAVE` en `.env` (o `Jwt__Clave` en desarrollo). |
| La API se cierra con `Falta ConnectionStrings:DefaultConnection` | En desarrollo no definiste la variable de entorno de la cadena de conexión. |
| Error de login de SQL Server | `DB_SA_PASSWORD` no cumple la política de complejidad, o contiene `;`. Si cambiaste la clave después de crear el volumen, la vieja sigue vigente: `docker compose down -v` (borra los datos) o usá la anterior. |
| No aparecen provincias/ciudades al cargar un domicilio | No se pudo descargar georef. Administrador → *Panel general → Cargar geografía*. |
| El frontend en `npm run dev` da error 502 / de red en `/api` | La API no está corriendo en el puerto 5000, o falta `VITE_API_URL`. |
| Cambié permisos de un rol y el usuario no los ve | Los permisos viajan en el token: el usuario tiene que cerrar sesión y volver a ingresar. |

## Historial de cambios
En `textos/` están las notas de cada cambio importante hecho durante el desarrollo:
- `CAMBIOS_rendimiento.md` — paginado en el servidor, Dashboard por SQL, índices, cache, PDF diferido.
- `CAMBIOS_script_unico_bd.md` — unificación de los scripts de base de datos.
- `CAMBIOS_healthcheck_db.md` — healthcheck más tolerante para equipos con poca RAM.
- `README.md` — versión anterior de este README.
