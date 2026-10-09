# Huella de Carbono (HDC)

Aplicación web para calcular la huella de carbono de individuos y organizaciones a partir de sus consumos mensuales.

- **Frontend:** React 19 + Vite + Bootstrap 5 + Chart.js + jsPDF (`frontend/`)
- **Backend:** .NET 9 / C# en 4 capas (`backend/`)
- **Base de datos:** SQL Server
- **Orquestación:** Docker Compose

```
HDC/
├─ docker-compose.yml        db (SQL Server) + api (.NET) + frontend (nginx)
├─ .env.example              variables (copiar a .env)
├─ backend/
│  ├─ HDC.Entity/            Capa de entidades: entidades del ORM + DTOs
│  ├─ HDC.DAL/               Acceso a datos: DbContext (EF Core), repositorios, scripts SQL
│  ├─ HDC.BLL/               Lógica de negocio: servicios, reglas, JWT, semillas
│  └─ HDC.API/               Presentación: controladores REST, auth, manejo de errores
└─ frontend/                 SPA React (servida por nginx, que hace de proxy a /api)
```

## Levantar el proyecto

1. Copiá `.env.example` a `.env` y completá las contraseñas (ya hay un `.env` de desarrollo).
2. `docker compose up -d --build`
3. Abrí http://localhost:8080 — API/Swagger en http://localhost:5000/swagger

Al primer arranque la API:
- crea la base y ejecuta `HDC.DAL/Scripts/01_esquema.sql` y `02_datos_iniciales.sql`;
- crea el administrador (`ADMIN_EMAIL` / `ADMIN_CONTRASENA` del `.env`);
- descarga en segundo plano provincias, departamentos y localidades desde la API **georef** (24 / 529 / ~4.000).
  Si falla (sin internet), se reintenta desde *Panel de administración → Cargar geografía*.

### Requisitos de memoria

SQL Server en contenedor necesita **al menos 2 GB de RAM** asignados a Docker. En equipos con 4 GB totales
conviene cerrar otras aplicaciones o usar la instancia local de SQL Server (ver abajo).

### Usar el SQL Server instalado en Windows en lugar del contenedor

1. Habilitá TCP/IP en *SQL Server Configuration Manager* y la autenticación SQL (modo mixto); creá un login con permiso para crear bases.
2. En `docker-compose.yml` eliminá el servicio `db` (y el `depends_on` de `api`) y cambiá la cadena de conexión:
   `Server=host.docker.internal,1433;Database=HuellaCarbono;User Id=<usuario>;Password=<clave>;TrustServerCertificate=True;`

`Trusted_Connection=True` (autenticación de Windows) no funciona desde un contenedor Linux.

### Desarrollo sin Docker para el frontend

```
cd frontend
npm install
npm run dev        # http://localhost:5173, redirige /api a http://localhost:5000
```

## Cambios al esquema original

| Cambio | Motivo |
|---|---|
| PK con `IDENTITY` (salvo PROVINCIA/DISTRITO/CIUDAD, que usan el id de georef) | IDs generados por la base |
| `CIUDAD.NOMBRE` BIGINT → NVARCHAR | Era un error de tipo |
| `FECHA_BAJA` admite NULL | Activo = sin fecha de baja |
| `REGISTROS` pasa a cabecera (domicilio + período + usuario que cargó) con `DETALLE_REGISTRO` (fuente, consumo, factor, kg CO₂) | Un registro mensual tiene varios consumos; el factor se congela para no alterar el histórico |
| `ROL_PERMISO` (N:M) | Un permiso puede pertenecer a varios tipos de usuario |
| `INDIVIDUO` + DNI, género · `ORGANIZACION` + descripción, cantidad de miembros · `DOMICILIO` + block, manzana, teléfono · `VEHICULO` | Datos del glosario |
| `DOMICILIO_FUENTE` | Fuentes habituales del domicilio (se precargan al crear un registro) |
| `COLABORACION` + `COLABORACION_TIPO_EMISION` (reemplaza `COLABORA`) | Ver colaboradores |
| `REPORTE` | Guardar combinaciones de filtros |
| Unidades `kWh` y `km` | Necesarias para electricidad y transporte |

Los factores de emisión iniciales son **valores de referencia**: revisalos con la fuente oficial que uses.

## Colaboradores

1. El titular (individuo u organización) invita por email a un usuario registrado, para un **domicilio**, un **rango de períodos**
   y los **tipos de emisión** que puede cargar (por ejemplo, solo "Transporte de colaboradores").
2. El invitado acepta o rechaza. Con la colaboración aceptada y vigente, carga consumos solo de esos tipos.
3. Esos registros quedan marcados con quién los cargó y **suman a la huella del domicilio del titular**; el dashboard los muestra en "Aportes de colaboradores".
4. Cualquiera de los dos puede finalizar la colaboración; los registros ya cargados se conservan.

## Cálculo

- `kg CO₂ = consumo × factor de emisión` (el factor queda guardado en el detalle).
- Los totales se muestran en **toneladas de CO₂**, junto con su equivalente atmosférico: `1 ppm = 17.600 millones de t CO₂`.

## Seguridad y permisos

JWT con un claim por permiso. Las pantallas y los endpoints se habilitan por **permiso**, no por nombre de rol, así que el administrador
puede crear tipos de usuario nuevos y asignarles permisos. Los permisos del sistema y los 3 roles iniciales están protegidos contra borrado.
Los cambios de permisos se aplican en el siguiente inicio de sesión.
