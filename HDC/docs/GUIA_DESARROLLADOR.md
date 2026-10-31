# Guía del desarrollador — Huella de Carbono (HDC)

> **Última actualización:** 31/10/2026 · Versión del sistema: 2.2 (ver [CHANGELOG](CHANGELOG.md))

Cómo levantar el proyecto, trabajarlo como desarrollador y ponerlo en producción.
La arquitectura y las decisiones técnicas están en la [Documentación técnica](DOCUMENTACION_TECNICA.md).

## Índice
1. [Requisitos](#1-requisitos)
2. [Levantar todo con Docker](#2-levantar-todo-con-docker)
3. [Usuarios y datos de prueba](#3-usuarios-y-datos-de-prueba)
4. [Variables de entorno](#4-variables-de-entorno)
5. [Trabajar en desarrollo (sin Docker para API y frontend)](#5-trabajar-en-desarrollo-sin-docker-para-api-y-frontend)
6. [Emails en desarrollo (Mailpit)](#6-emails-en-desarrollo-mailpit)
7. [Cambios en la base de datos](#7-cambios-en-la-base-de-datos)
8. [Convenciones del código](#8-convenciones-del-código)
9. [Puesta en producción](#9-puesta-en-producción)
10. [Problemas frecuentes](#10-problemas-frecuentes)
11. [Cómo mantener esta documentación](#11-cómo-mantener-esta-documentación)

---

## 1. Requisitos

| Para…                         | Necesitás                                                        |
|-------------------------------|------------------------------------------------------------------|
| Solo levantar el sistema      | **Docker Desktop** (Windows/Mac) o Docker Engine + Compose (Linux) |
| Trabajar en el backend        | **.NET SDK 9** (`dotnet --version` → 9.x)                        |
| Trabajar en el frontend       | **Node.js 22 o superior** + npm                                  |
| Ver la base de datos (opcional) | pgAdmin, DBeaver o `psql`                                      |
| Editores sugeridos            | Visual Studio 2022 / Rider (backend), VS Code (frontend)         |

Memoria: con PostgreSQL alcanza con **~1 GB de RAM libre** para Docker (antes, SQL Server pedía 2 GB).
La primera vez hace falta internet para descargar imágenes, paquetes y la geografía argentina.

---

## 2. Levantar todo con Docker

Desde la carpeta `HDC/`:

```bash
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
# editá .env: como mínimo DB_CONTRASENA, JWT_CLAVE y ADMIN_CONTRASENA
docker compose up -d --build
```

Se levantan 4 contenedores:

| Servicio   | Qué es                                   | Dirección                                   |
|------------|------------------------------------------|---------------------------------------------|
| `frontend` | La aplicación web (nginx)                | http://localhost:8081                       |
| `api`      | API REST .NET 9                          | http://localhost:5000/swagger · `/api/salud` |
| `db`       | PostgreSQL 17                            | `localhost:5433` (solo desde esta PC)       |
| `mailpit`  | Servidor de correo de prueba             | http://localhost:8025                       |

La **primera compilación tarda varios minutos** (descarga las imágenes de .NET y Node y restaura paquetes).
Las siguientes son mucho más rápidas porque Docker reutiliza lo ya descargado. Si no cambiaste código,
usá `docker compose up -d` **sin** `--build`.

### Qué pasa en el arranque de la API
1. Crea la base (si no existe) y ejecuta el script único `backend/HDC.DAL/Scripts/HDC_base_datos.sql`
   (tablas, datos iniciales, índices). Es **idempotente**: corre en cada arranque y solo crea lo que falta.
2. Crea el **administrador** con `ADMIN_EMAIL` / `ADMIN_CONTRASENA` (solo si no hay ninguno).
3. En segundo plano descarga provincias, distritos y localidades desde la API pública **georef**
   (24 / ~530 / ~4.000; tarda alrededor de un minuto).
4. Después de la geografía, si `DATOS_PRUEBA=true`, carga los **datos de prueba** (sección 3).

### Comandos útiles

| Acción                                     | Comando                           |
|--------------------------------------------|-----------------------------------|
| Estado de los contenedores                 | `docker compose ps`               |
| Logs de la API (ver errores, emails, etc.) | `docker compose logs -f api`      |
| Detener todo (los datos se conservan)      | `docker compose down`             |
| Levantar sin recompilar                    | `docker compose up -d`            |
| Recompilar después de cambiar código       | `docker compose up -d --build`    |
| Recompilar solo la API                     | `docker compose up -d --build api`|
| **Borrar todo, incluida la base**          | `docker compose down -v`          |
| Consola SQL dentro del contenedor          | `docker compose exec db psql -U hdc -d huellacarbono` |

Los datos de PostgreSQL se guardan en el volumen `pgdata`: sobreviven a `down`/`up`; solo `down -v` los borra.

---

## 3. Usuarios y datos de prueba

Con `DATOS_PRUEBA=true` en el `.env`, la API crea estas cuentas la primera vez que arranca
(unos segundos después de que termina de descargar la geografía):

| Cuenta                         | Tipo          | Contraseña               | Qué tiene                                                   |
|--------------------------------|---------------|--------------------------|-------------------------------------------------------------|
| `ADMIN_EMAIL` (`admin@hdc.com`)| Administrador | `ADMIN_CONTRASENA` del `.env` | Panel de administración completo                        |
| `maria.gomez@prueba.com`       | Individuo     | `Prueba2026`             | Casa con 12 meses de consumos; Juan colabora cargando transporte; invitación pendiente de EcoSur |
| `juan.perez@prueba.com`        | Individuo     | `Prueba2026`             | Casa con 12 meses de consumos; colabora en la casa de María |
| `contacto@ecosur.prueba.com`   | Organización  | `Prueba2026`             | Planta industrial con 12 meses de consumos; invitó a María; convocatoria **ECSR-2K26** |
| `empleado01@ecosur.prueba.com` … `empleado30@…` | Individuo | `Prueba2026` | Miembros de la convocatoria de EcoSur: 01–20 activos con consumos, 21–27 esperando aprobación, 28–30 dados de baja |

- Los consumos son inventados pero realistas: más gas en invierno, más electricidad en verano. Así el
  Dashboard, los gráficos y los reportes muestran variaciones.
- Para probar las convocatorias: entrá como `maria.gomez@prueba.com` → Colaboraciones → **Sumarme con un código** →
  `ECSR-2K26`. Después entrá como EcoSur → Colaboraciones → Convocatorias → **Colaboradores** para aprobarla.
- Es idempotente por bloques: si `maria.gomez@prueba.com` ya existe no se recrean los usuarios; si la
  convocatoria `ECSR2K26` ya existe no se recrea. Por eso una base de una versión anterior recibe los bloques nuevos.
- Para **regenerarlos**: `docker compose down -v` (borra la base) y `docker compose up -d`.
- El código está en `backend/HDC.BLL/Servicios/DatosPruebaServicio.cs`.
- **En producción `DATOS_PRUEBA` debe ser `false`.**

---

## 4. Variables de entorno

Se definen en `HDC/.env` (copia de `.env.example`, **no se sube a git**). `docker-compose.yml` las pasa a
la API como configuración de .NET (`Seccion__Clave` equivale a `Seccion:Clave` de `appsettings.json`).

| Variable                 | Configuración .NET              | Para qué sirve                                                       | Por defecto |
|--------------------------|---------------------------------|----------------------------------------------------------------------|-------------|
| `DB_USUARIO`             | (cadena de conexión)            | Usuario de PostgreSQL                                                | `hdc` |
| `DB_CONTRASENA`          | (cadena de conexión)            | Contraseña de PostgreSQL. **Sin `;`**                                | — |
| `DB_NAME`                | (cadena de conexión)            | Nombre de la base                                                    | `huellacarbono` |
| `DB_PORT`                | —                               | Puerto publicado en la PC (solo `127.0.0.1`)                         | `5433` |
| `JWT_CLAVE`              | `Jwt:Clave`                     | Firma de las sesiones. **Mínimo 32 caracteres** o la API no arranca  | — |
| `JWT_EXPIRA_MINUTOS`     | `Jwt:ExpiraMinutos`             | Duración de la sesión                                                | `120` |
| `ADMIN_EMAIL`            | `Admin:Email`                   | Email del administrador inicial                                      | `admin@hdc.com` |
| `ADMIN_CONTRASENA`       | `Admin:Contrasena`              | Contraseña del administrador inicial (solo se usa la primera vez)    | — |
| `DATOS_PRUEBA`           | `DatosPrueba:Habilitado`        | Cargar los datos de prueba                                           | `false` |
| `SMTP_HOST`              | `Email:Host`                    | Servidor SMTP                                                        | `mailpit` |
| `SMTP_PUERTO`            | `Email:Puerto`                  | Puerto SMTP                                                          | `1025` |
| `SMTP_USUARIO`           | `Email:Usuario`                 | Usuario SMTP (vacío = sin autenticación, como Mailpit)               | — |
| `SMTP_CONTRASENA`        | `Email:Contrasena`              | Contraseña / API key SMTP                                            | — |
| `SMTP_SEGURIDAD`         | `Email:Seguridad`               | `Ninguna` · `StartTls` (587) · `SslOnConnect` (465) · `Auto`         | `Ninguna` |
| `EMAIL_REMITENTE`        | `Email:Remitente`               | Dirección "De:" de los emails                                        | `no-responder@hdc.local` |
| `EMAIL_NOMBRE_REMITENTE` | `Email:NombreRemitente`         | Nombre "De:" de los emails                                           | `Huella de Carbono` |
| `URL_PUBLICA`            | `Recuperacion:UrlFrontend`      | Dirección pública del sistema (para armar el link del email)         | `http://localhost:8081` |
| `RECUPERACION_MINUTOS`   | `Recuperacion:MinutosValidez`   | Validez del link de recuperación (entre 5 y 1440)                    | `30` |
| `API_PORT`               | —                               | Puerto de la API en la PC                                            | `5000` |
| `FRONTEND_PORT`          | —                               | Puerto de la web en la PC                                            | `8081` |
| `MAILPIT_PORT`           | —                               | Puerto de la bandeja de Mailpit                                      | `8025` |
| `SWAGGER`                | `Swagger:Habilitado`            | Publicar `/swagger`                                                  | `true` |

Otras opciones de `appsettings.json` sin variable propia: `Georef:Url`, `Cors:Origenes`.

---

## 5. Trabajar en desarrollo (sin Docker para API y frontend)

Conviene correr la API con `dotnet run` y el frontend con `npm run dev`: los cambios se ven al instante
y se usa menos memoria. La base y el correo pueden seguir en Docker:

```bash
docker compose up -d db mailpit
```

### 5.1 Backend (API)
La API necesita valores que **no** están en `appsettings.json` por seguridad. Pasalos como variables de
entorno (`__` reemplaza a `:`). PowerShell, desde `HDC/backend`:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=huellacarbono;Username=hdc;Password=<DB_CONTRASENA>"
$env:Jwt__Clave = "una-clave-de-desarrollo-de-al-menos-32-caracteres"
$env:Admin__Contrasena = "Admin2026!"
$env:DatosPrueba__Habilitado = "true"
$env:Email__Host = "localhost"; $env:Email__Puerto = "1025"; $env:Email__Seguridad = "Ninguna"
dotnet run --project HDC.API --urls http://localhost:5000
```

Bash:
```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5433;Database=huellacarbono;Username=hdc;Password=<DB_CONTRASENA>'
export Jwt__Clave='una-clave-de-desarrollo-de-al-menos-32-caracteres'
export Admin__Contrasena='Admin2026!'
export DatosPrueba__Habilitado=true
export Email__Host=localhost Email__Puerto=1025 Email__Seguridad=Ninguna
dotnet run --project HDC.API --urls http://localhost:5000
```

- Puerto **5000**: es al que apunta el proxy de Vite. Si usás el perfil de `launchSettings.json`
  (puerto 5081, por ejemplo desde Visual Studio), iniciá el frontend con `VITE_API_URL=http://localhost:5081`.
- En desarrollo el link de recuperación apunta a `http://localhost:5173` (valor de `appsettings.json`).
- Compilar sin ejecutar: `dotnet build HDC.sln`.
- Si preferís un PostgreSQL instalado en la PC, cambiá `Host`/`Port`/credenciales de la cadena de conexión.
  No hace falta crear la base ni correr scripts: la API lo hace al arrancar.

### 5.2 Frontend
```bash
cd frontend
npm install
npm run dev          # http://localhost:5173 — redirige /api a http://localhost:5000
```

| Script            | Qué hace                                       |
|-------------------|------------------------------------------------|
| `npm run dev`     | Servidor de desarrollo con recarga en caliente |
| `npm run build`   | Versión de producción en `dist/`               |
| `npm run preview` | Sirve `dist/` localmente para probarla         |

---

## 6. Emails en desarrollo (Mailpit)

La recuperación de contraseña envía emails. En desarrollo **no se envían de verdad**: los atrapa
**Mailpit** y se ven en **http://localhost:8025**.

Para probar el flujo completo:
1. En http://localhost:8081/login → **¿Olvidaste tu contraseña?** → escribí `maria.gomez@prueba.com`.
2. Abrí http://localhost:8025, abrí el email "Recuperá tu contraseña" y hacé click en el botón.
3. Escribí la contraseña nueva. En Mailpit aparece un segundo email avisando el cambio.

Si el email no aparece: `docker compose logs api | grep -i email` muestra si hubo un error de SMTP.

---

## 7. Cambios en la base de datos

Todo el esquema está en **un único script**: `backend/HDC.DAL/Scripts/HDC_base_datos.sql`
(secciones: 1. esquema, 2. datos iniciales, 3. índices). Va embebido en la DLL y se ejecuta en **cada
arranque**, en una transacción. Por eso cada sentencia tiene que poder ejecutarse muchas veces:

```sql
-- Tabla nueva (nombres en minúscula, sin comillas)
CREATE TABLE IF NOT EXISTS nueva_tabla (
    id_nueva BIGINT GENERATED BY DEFAULT AS IDENTITY,
    nombre   VARCHAR(100) NOT NULL,
    CONSTRAINT pk_nueva_tabla PRIMARY KEY (id_nueva)
);

-- Columna nueva en una tabla existente
ALTER TABLE domicilio ADD COLUMN IF NOT EXISTS nueva_columna VARCHAR(50) NULL;

-- Índice nuevo
CREATE INDEX IF NOT EXISTS ix_nuevo ON tabla (columna);

-- Datos iniciales (solo si la tabla está vacía)
INSERT INTO nueva_tabla (nombre)
SELECT v.* FROM (VALUES ('Uno'), ('Dos')) AS v(nombre)
WHERE NOT EXISTS (SELECT 1 FROM nueva_tabla);
```

Después:
1. Agregá o modificá la entidad en `backend/HDC.Entity/Entities/`.
2. Mapeala en `backend/HDC.DAL/HdcDbContext.cs` (nombres en MAYÚSCULA como el resto; el método
   `AMinusculas` los convierte a minúscula). Las fechas con hora llevan
   `.HasColumnType("timestamp(0) without time zone")` o `timestamp(3)`.
3. Registrá el cambio en [CHANGELOG.md](CHANGELOG.md) y, si cambia el modelo, en la
   [Documentación técnica](DOCUMENTACION_TECNICA.md#4-modelo-de-datos).

Diferencias con SQL Server que hay que tener en cuenta al escribir consultas:
- `LIKE` **distingue mayúsculas**: para buscar texto usá `EF.Functions.ILike(columna, ExtensionesConsulta.PatronContiene(texto))`.
- `==` entre textos también distingue mayúsculas: para comparar nombres usá `x.Nombre.ToLower() == valor.ToLower()`.
- Las fechas `DateTime` se guardan **sin zona horaria** (`timestamp without time zone`) y en hora de Argentina
  (`DateTime.Now`). No guardes `DateTime.UtcNow` en esas columnas: Npgsql lo rechaza.

---

## 8. Convenciones del código

- **Idioma:** nombres de clases, métodos, variables y mensajes en **español** (como el dominio).
- **Capas:** controlador → servicio (BLL) → repositorio (DAL). Los controladores no tienen lógica; los
  servicios no usan el `DbContext` directamente (excepción documentada: `DatosPruebaServicio`).
- **Errores de negocio:** lanzar `NegocioException`, `ProhibidoException`, `NoEncontradoException`,
  `ConflictoException` o `NoAutorizadoException`; el middleware los traduce a HTTP con `{ mensaje }`.
- **Validaciones de entrada:** con atributos en los DTOs (`HDC.Entity/Dtos`, reglas en `Validaciones/Reglas.cs`).
  Las mismas reglas existen en el frontend (`components/comunes/Formulario.jsx`, objeto `reglas`).
- **Permisos:** endpoints y pantallas se protegen por **permiso**, no por nombre de rol.
- **Comentarios:** cada clase y método no trivial lleva un comentario que explica *qué hace y por qué*.
- **Frontend:** una página por archivo en `src/pages/{publicas,usuario,admin}`, cargada con `lazy()` en `App.jsx`;
  llamadas a la API solo desde `src/api/servicios.js`.
- **Commits:** mensajes en español que digan qué cambió. Cada cambio funcional se anota en el CHANGELOG.

---

## 9. Puesta en producción

Lista de control antes de publicar:

- [ ] `.env` con **contraseñas nuevas y largas** (`DB_CONTRASENA`, `ADMIN_CONTRASENA`) y una `JWT_CLAVE`
      aleatoria de 64 caracteres o más (por ejemplo `openssl rand -hex 32`).
- [ ] `DATOS_PRUEBA=false`.
- [ ] `SWAGGER=false` (o dejarlo solo si la API no es accesible desde internet).
- [ ] **SMTP real** configurado (ver abajo) y probado con "¿Olvidaste tu contraseña?".
- [ ] `URL_PUBLICA` con el dominio real, con **https** (por ejemplo `https://huella.midominio.com.ar`).
- [ ] HTTPS delante del frontend (proxy inverso con certificado, por ejemplo Caddy, Traefik o nginx + Let's Encrypt).
- [ ] No publicar el puerto de la API (`API_PORT`) a internet: el frontend ya la expone en `/api` a través de nginx.
- [ ] Copias de seguridad de la base:
      `docker compose exec db pg_dump -U hdc huellacarbono > respaldo_$(date +%F).sql`
- [ ] Revisar los factores de emisión iniciales con la fuente oficial que se vaya a usar.

### Proveedor de email (SMTP)
Recomendación: un **servicio de email transaccional** con un remitente de un **dominio propio o institucional**
que tenga **SPF y DKIM** configurados; si no, los emails suelen terminar en spam. Opciones:

| Proveedor | Plan gratuito aprox. | Configuración típica |
|-----------|----------------------|----------------------|
| Brevo     | 300 emails/día       | `smtp-relay.brevo.com`, 587, `StartTls` |
| Amazon SES| pago por uso (muy bajo) | `email-smtp.<región>.amazonaws.com`, 587, `StartTls` |
| Mailgun / Resend | planes de prueba | 587, `StartTls` |
| Servidor SMTP de la institución | — | consultar al área de sistemas |

No se recomienda una cuenta personal de Gmail: tiene límite diario, exige contraseña de aplicación y la
entrega es poco confiable. Para cambiar de proveedor solo se tocan las variables `SMTP_*` y `EMAIL_*`.

Con un SMTP real el servicio `mailpit` queda sin uso. Se puede dejar (solo escucha en `127.0.0.1`) o
quitarlo del `docker-compose.yml`.

---

## 10. Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| `docker compose up --build` tarda mucho | La primera vez descarga imágenes (.NET SDK ~1 GB) y paquetes. Las siguientes son rápidas. Sin cambios de código, usá `up -d` sin `--build`. |
| La API se cierra con `Jwt:Clave debe tener al menos 32 caracteres` | Alargá `JWT_CLAVE` en `.env` (o `Jwt__Clave` en desarrollo). |
| La API se cierra con `Falta ConnectionStrings:DefaultConnection` | En desarrollo falta la variable de entorno de la cadena de conexión. |
| `password authentication failed for user "hdc"` | La contraseña del `.env` no coincide con la del volumen. PostgreSQL toma `DB_CONTRASENA` **solo al crear el volumen**: si la cambiaste después, usá la anterior o `docker compose down -v` (borra los datos). |
| No aparecen provincias/ciudades | No se pudo descargar georef. Administrador → *Panel general → Cargar geografía*. |
| No aparecen los usuarios de prueba | `DATOS_PRUEBA` no está en `true`, o georef todavía no terminó (se cargan después). Revisá `docker compose logs api`. |
| No llega el email de recuperación | Desarrollo: miralo en http://localhost:8025. Producción: revisá `SMTP_*` y `docker compose logs api` ("No se pudo enviar el email"). |
| El link del email apunta a otra dirección | Ajustá `URL_PUBLICA`. |
| "Hiciste demasiados intentos" | Límite de 5 pedidos cada 15 minutos por IP en la recuperación. Esperá o reiniciá la API (`docker compose restart api`). |
| El frontend en `npm run dev` da error de red en `/api` | La API no está en el puerto 5000, o falta `VITE_API_URL`. |
| Cambié permisos de un rol y el usuario no los ve | Los permisos viajan en el token: hay que cerrar sesión y volver a entrar. |

---

## 11. Cómo mantener esta documentación

La documentación está toda en `HDC/docs/` y se organiza así:

| Archivo | Para quién | Se actualiza cuando… |
|---|---|---|
| `README.md` (raíz) | Todos: portada e índice | Cambia la forma de arrancar o se agrega un documento |
| `docs/MANUAL_USUARIO.md` | Usuarios finales | Cambia una pantalla o una funcionalidad visible |
| `docs/GUIA_DESARROLLADOR.md` | Desarrolladores | Cambia la instalación, variables, comandos o convenciones |
| `docs/DOCUMENTACION_TECNICA.md` | Equipo técnico | Cambia la arquitectura, el modelo de datos, la API o la seguridad |
| `docs/CHANGELOG.md` | Todos | **Siempre**: cada cambio agrega una entrada |

Reglas:
1. **No crear archivos sueltos de cambios.** Cada cambio se registra como una entrada nueva al principio de
   `CHANGELOG.md` (fecha, qué, por qué, archivos tocados) y se actualiza la sección del documento afectado.
2. Al editar un documento, actualizar su línea **"Última actualización"**.
3. Si cambia la versión del sistema, actualizarla en el CHANGELOG y en el encabezado de los documentos.
