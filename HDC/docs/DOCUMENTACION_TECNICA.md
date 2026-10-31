# Documentación técnica — Huella de Carbono (HDC)

> **Última actualización:** 31/10/2026 · Versión del sistema: 2.1 (ver [CHANGELOG](CHANGELOG.md))

Descripción técnica del sistema para el equipo de desarrollo: arquitectura, tecnologías, modelo de datos,
API, seguridad y decisiones de diseño. Para instalarlo y trabajarlo, ver la
[Guía del desarrollador](GUIA_DESARROLLADOR.md).

## Índice
1. [Qué hace el sistema](#1-qué-hace-el-sistema)
2. [Tecnologías](#2-tecnologías)
3. [Arquitectura](#3-arquitectura)
4. [Modelo de datos](#4-modelo-de-datos)
5. [API REST](#5-api-rest)
6. [Seguridad](#6-seguridad)
7. [Recuperación de contraseña](#7-recuperación-de-contraseña)
8. [Rendimiento](#8-rendimiento)
9. [Conceptos del dominio](#9-conceptos-del-dominio)
10. [Decisiones de diseño](#10-decisiones-de-diseño)

---

## 1. Qué hace el sistema

Aplicación web para que **individuos y organizaciones** calculen su **huella de carbono** a partir de sus
consumos mensuales (gas, electricidad, combustible, transporte, residuos, agua…), por domicilio.
Ofrece dashboard con estadísticas, reportes en PDF, colaboración entre usuarios para cargar datos y un panel
de administración para gestionar factores de emisión, catálogos, usuarios, roles y permisos.

## 2. Tecnologías

| Parte          | Tecnología                                                          | Carpeta      |
|----------------|---------------------------------------------------------------------|--------------|
| Frontend       | React 19, Vite 6, React Router, Bootstrap 5, Chart.js, jsPDF, axios | `frontend/`  |
| Backend (API)  | .NET 9 / C#, ASP.NET Core, 4 capas, JWT, BCrypt                     | `backend/`   |
| Acceso a datos | Entity Framework Core 9 + Npgsql (proveedor PostgreSQL)             | `backend/HDC.DAL` |
| Base de datos  | **PostgreSQL 17**                                                   | script en `backend/HDC.DAL/Scripts` |
| Email          | MailKit (SMTP); Mailpit en desarrollo                               | `backend/HDC.BLL/Servicios/EmailServicio.cs` |
| Servidor web   | nginx (sirve el frontend y hace de proxy de `/api`)                 | `frontend/nginx.conf` |
| Orquestación   | Docker Compose: `db`, `mailpit`, `api`, `frontend`                  | `docker-compose.yml` |
| Datos externos | API georef (datos.gob.ar): provincias, departamentos y localidades  | — |

## 3. Arquitectura

```
 Navegador ──HTTP──▶ nginx (frontend) ──/api──▶ API .NET ──EF Core/Npgsql──▶ PostgreSQL
                         │                         │
                         └─ archivos de la SPA      ├──SMTP──▶ Mailpit / proveedor de email
                                                   └──HTTPS──▶ API georef (al iniciar)
```

En producción el navegador habla solo con nginx (mismo origen: no hace falta CORS). nginx sirve la SPA y
reenvía `/api/*` a la API, agregando la IP real del usuario en `X-Forwarded-For`.

### 3.1 Backend en 4 capas

```
HDC.API (Controllers)  →  HDC.BLL (Servicios)  →  HDC.DAL (Repositorios + EF Core)  →  PostgreSQL
              ↑ todas usan HDC.Entity (entidades, DTOs, validaciones)
```

| Proyecto     | Responsabilidad | Contenido principal |
|--------------|-----------------|---------------------|
| `HDC.Entity` | Modelo compartido | `Entities/` (clases del ORM), `Dtos/` (entrada/salida de la API), `Validaciones/Reglas.cs` |
| `HDC.DAL`    | Acceso a datos | `HdcDbContext` (mapeo), `Repositorios/`, `InicializadorBaseDatos`, `Scripts/HDC_base_datos.sql` |
| `HDC.BLL`    | Lógica de negocio | `Servicios/` (Auth, Recuperacion, Perfil, Domicilio, Registro, Colaboracion, Estadistica, Admin, Semilla, DatosPrueba, Email), `Comun/` (permisos, excepciones, mapeos, cache) |
| `HDC.API`    | HTTP | `Controllers/`, `Infraestructura/` (middleware de errores, límites de pedidos, controlador base), `Program.cs` |

Flujo de una petición: el controlador recibe el DTO (validado automáticamente por sus atributos), llama al
servicio, el servicio aplica reglas y usa repositorios, y el resultado vuelve como DTO en JSON.
Los repositorios de una petición comparten el mismo `DbContext` (unidad de trabajo).

**Manejo de errores:** los servicios lanzan excepciones de negocio; `ManejoErroresMiddleware` las traduce:

| Excepción / error | HTTP |
|---|---|
| `NegocioException`, validación de DTO | 400 `{ mensaje, errores }` |
| `NoAutorizadoException` | 401 |
| `ProhibidoException` | 403 |
| `NoEncontradoException` | 404 |
| `ConflictoException`, PostgreSQL 23503 (FK) y 23505 (UNIQUE) | 409 |
| Límite de pedidos superado | 429 |
| Cualquier otra | 500 (se registra en el log) |

**Arranque (`Program.cs`):** inicializa la base (script), crea el administrador, y en segundo plano
descarga la geografía de georef y luego los datos de prueba (si están habilitados).
`EnvioEmailsServicio` corre como servicio en segundo plano durante toda la vida de la API.

### 3.2 Frontend

| Carpeta | Contenido |
|---|---|
| `src/api/` | `cliente.js` (axios, token JWT, manejo de errores y sesión vencida), `servicios.js` (todas las llamadas) |
| `src/context/` | `AuthContext` (sesión y permisos), `PreferenciasContext` (tema, accesibilidad), `UIContext` (avisos) |
| `src/pages/publicas/` | Login, Registro, OlvideContrasena, RestablecerContrasena |
| `src/pages/usuario/` | Dashboard, Domicilios, MisEmisiones, RegistroForm/Detalle, FuentesEmision (sección informativa), Colaboraciones, Reportes, Perfil |
| `src/pages/admin/` | Panel, Fuentes, Usuarios, Roles, Permisos, Catálogos |
| `src/components/` | Componentes reutilizables (tablas, formularios, gráficos, layout) |
| `src/utils/` | Formatos, colores, PDF (carga diferida) |

Rutas protegidas por permiso en `App.jsx` (`<Protegida permisos={[...]}>`). Las páginas se cargan con
`lazy()` para que el primer ingreso descargue poco código.

Cuando varias rutas usan **el mismo componente** con distinta configuración (`Catalogos` para alcances,
unidades y tipos de emisión; `Reportes` propios y globales), cada ruta lleva una `key` distinta. Sin ella
React reutiliza el componente al navegar y conserva el estado de la sección anterior (ver CHANGELOG 2.1).

## 4. Modelo de datos

Motor **PostgreSQL**. El esquema completo está en `backend/HDC.DAL/Scripts/HDC_base_datos.sql` y es la
fuente de verdad (no se usan migraciones de EF). Tablas y columnas en minúscula.

| Grupo | Tabla | Descripción |
|---|---|---|
| Seguridad | `rol` | Tipos de usuario (Administrador, Individuo, Organización + los que cree el admin) |
| | `permisos` | Permisos que verifica el código (`HDC.BLL/Comun/Permisos.cs`) |
| | `rol_permiso` | Relación N:M rol–permiso |
| | `usuario` | Cuenta: email, hash BCrypt de la contraseña, rol, fechas de alta/baja |
| | `individuo` / `organizacion` | Perfil 1:1 del usuario según su tipo |
| | `recuperacion_contrasena` | Pedidos de "olvidé mi contraseña": hash del token, vencimiento, uso |
| Geografía | `provincia` → `distrito` → `ciudad` | Cargadas desde georef (usan el id de georef) |
| Catálogos | `alcance`, `unidad_medida`, `tipo_emision`, `fuente_emision` | Fuentes con su **factor de emisión** y una **descripción** de ayuda para el usuario |
| Emisiones | `domicilio` | Lugar de medición de un usuario; guarda el total acumulado de kg CO₂ |
| | `registros` | Cabecera: domicilio + período (día 1 del mes) + quién cargó |
| | `detalle_registro` | Consumo de una fuente: consumo, factor congelado, kg CO₂ |
| Colaboración | `colaboracion`, `colaboracion_tipo_emision` | Invitación a cargar ciertos tipos de emisión en un domicilio |
| Reportes | `reporte` | Filtros guardados (JSON) para regenerar un reporte |

Relaciones principales:

```
rol 1─N usuario 1─1 individuo | organizacion
usuario 1─N domicilio 1─N registros 1─N detalle_registro N─1 fuente_emision N─1 tipo_emision N─1 alcance
domicilio N─1 ciudad N─1 distrito N─1 provincia
domicilio 1─N colaboracion N─1 usuario (colaborador);  colaboracion 1─N colaboracion_tipo_emision
usuario 1─N recuperacion_contrasena
```

Convenciones del esquema:
- PK `BIGINT GENERATED BY DEFAULT AS IDENTITY` (salvo geografía, con id de georef).
- Fechas con hora: `TIMESTAMP(0)` o `TIMESTAMP(3)` **sin zona horaria**, en hora de Argentina.
- Baja lógica con `fecha_baja` (NULL = activo) en usuarios, domicilios y fuentes.
- Índices sobre todas las FK usadas en filtros y uniones (PostgreSQL no los crea solo), con `INCLUDE` para
  resolver consultas frecuentes leyendo solo el índice.

Datos iniciales del script: 3 roles, 12 permisos, 3 alcances, 5 unidades, 8 tipos de emisión y 15 fuentes.

## 5. API REST

Base: `/api`. Formato JSON. Autenticación con `Authorization: Bearer <token>` salvo los endpoints públicos.
La documentación interactiva completa está en **`/swagger`**.

| Recurso | Endpoints | Acceso |
|---|---|---|
| Autenticación | `POST auth/login`, `POST auth/registro/individuo`, `POST auth/registro/organizacion`, `GET auth/sesion` | Público (salvo `sesion`) |
| Recuperación  | `POST auth/recuperar`, `POST auth/restablecer` | Público, con límite por IP |
| Perfil        | `GET/PUT perfil`, `PUT perfil/contrasena` | Usuario autenticado |
| Geografía     | `GET geo/provincias`, `GET geo/provincias/{id}/distritos`, `GET geo/distritos/{id}/ciudades` | Público |
| Domicilios    | `GET/POST domicilios`, `GET/PUT/DELETE domicilios/{id}` | `GESTIONAR_DOMICILIOS` |
| Registros     | `GET/POST registros` (paginado), `GET registros/domicilios`, `GET/PUT/DELETE registros/{id}` | `GESTIONAR_EMISIONES` o `COLABORAR` |
| Colaboraciones| `GET enviadas/recibidas`, `POST`, `PUT {id}`, `POST {id}/aceptar · rechazar · revocar` | `GESTIONAR_COLABORACIONES` / `COLABORAR` |
| Dashboard     | `GET dashboard` | `VER_DASHBOARD` |
| Reportes      | `POST reportes/generar`, `POST reportes/global/generar`, `GET/POST/DELETE reportes/guardados` | `GENERAR_REPORTES` / `REPORTES_GLOBALES` |
| Catálogos     | `alcances`, `unidades`, `tipos-emision`, `fuentes` (CRUD), `POST catalogos/georef/sincronizar` | Lectura: autenticado · Escritura: `GESTIONAR_CATALOGOS` / `GESTIONAR_FUENTES` |
| Roles y permisos | CRUD `roles`, CRUD `permisos` | `GESTIONAR_ROLES` |
| Usuarios (admin) | `GET usuarios` (paginado), `GET usuarios/resumen`, `GET usuarios/{id}`, `PUT usuarios/{id}/estado` | `GESTIONAR_USUARIOS` |
| Salud | `GET salud` | Público |

Listados paginados (`registros`, `usuarios`): devuelven `{ items, total }`; tamaños 10, 20, 50 o 100.

## 6. Seguridad

- **Contraseñas:** hash **BCrypt** (factor 11). Nunca se guardan ni se registran en texto plano.
- **Sesión:** JWT firmado con HMAC (`Jwt:Clave`, mínimo 32 caracteres), con emisor, audiencia y vencimiento
  (`Jwt:ExpiraMinutos`). El token lleva un claim por permiso.
- **Autorización por permisos**, no por rol: cada endpoint exige una política con el permiso correspondiente,
  y el frontend oculta lo que el usuario no puede usar. El administrador puede crear roles nuevos.
- **Validación** de todos los datos de entrada con atributos en los DTOs (misma regla en el frontend).
- **Errores:** las respuestas nunca incluyen detalles internos; el detalle queda en el log de la API.
- **Límite de pedidos** (429) en los endpoints públicos de recuperación de contraseña.
- **Base de datos y Mailpit** publicados solo en `127.0.0.1` (no accesibles desde otra máquina).
- **Secretos** solo en `.env` / variables de entorno (el `.env` está en `.gitignore`).

Limitación conocida: el JWT es *stateless*; al cambiar o restablecer la contraseña, una sesión ya abierta en
otro dispositivo sigue válida hasta que vence su token (por defecto 2 horas).

## 7. Recuperación de contraseña

Archivos: `HDC.BLL/Servicios/RecuperacionServicio.cs`, `HDC.BLL/Servicios/EmailServicio.cs`,
`HDC.API/Infraestructura/LimitesPedidos.cs`, `HDC.API/Controllers/AuthController.cs`,
`frontend/src/pages/publicas/OlvideContrasena.jsx` y `RestablecerContrasena.jsx`.

```
Usuario            Frontend                     API                                 Base / Email
  │ "Olvidé mi clave" │                            │                                     │
  │──email──────────▶ │──POST /auth/recuperar────▶ │ busca usuario activo                │
  │                   │                            │ invalida links anteriores           │
  │                   │                            │ token = 32 bytes aleatorios (hex)   │
  │                   │                            │ guarda SHA-256(token), vence +30 min├──▶ recuperacion_contrasena
  │                   │                            │ encola email con link ?token=…      ├──▶ cola → SMTP
  │                   │ ◀── mensaje genérico ──────│ (mismo mensaje exista o no)         │
  │ abre el link del email                         │                                     │
  │──nueva clave────▶ │──POST /auth/restablecer──▶ │ busca por SHA-256(token)            │
  │                   │                            │ ¿existe, no usado, no vencido?      │
  │                   │                            │ BCrypt(nueva) → usuario.contrasena  │
  │                   │                            │ marca usado + invalida pendientes   │
  │                   │ ◀── "contraseña cambiada" ─│ encola email de confirmación        │
```

Decisiones de seguridad:

| Medida | Por qué |
|---|---|
| Token de 256 bits con `RandomNumberGenerator` | Imposible de adivinar por fuerza bruta |
| Se guarda solo el hash SHA-256 del token | Si se filtra la base, los links enviados no sirven |
| Vence (30 min, configurable) y es de un solo uso | Limita la ventana de abuso si alguien accede al email más tarde |
| Un solo link vigente por usuario | Pedir uno nuevo anula los anteriores |
| Respuesta idéntica exista o no el email | No permite averiguar qué emails están registrados |
| Envío por cola en segundo plano | La respuesta no espera al SMTP (no se filtra por tiempo de respuesta) y un SMTP lento no bloquea la API |
| Límite de 5 pedidos cada 15 min por IP | Evita inundar de emails a un usuario y probar tokens al azar |
| Email de confirmación al cambiar la contraseña | Si no fue el titular, se entera |
| Cuentas deshabilitadas no reciben link ni pueden restablecer | La baja del administrador se respeta |

Envío de emails: `ColaEmails` (canal acotado a 500 mensajes, Singleton) + `EnvioEmailsServicio`
(`BackgroundService`, 3 intentos con espera creciente). El proveedor SMTP se configura en la sección
`Email` (ver variables en la guía del desarrollador). La cola es en memoria: si la API se reinicia con
emails pendientes, esos emails se pierden y el usuario debe volver a pedir el link.

## 8. Rendimiento

- **Paginado y filtrado en la base** (`OFFSET/LIMIT`), con `COUNT` y `SUM` en SQL.
- **Dashboard y reportes** calculados con `GROUP BY` en la base, no en memoria.
- **Índices** en todas las FK de filtros frecuentes, con columnas `INCLUDE`.
- **Cache en memoria** de catálogos y geografía (`CacheCatalogos`, 1 hora o hasta que se modifiquen).
- **Compresión** Brotli/gzip de las respuestas JSON.
- **Frontend:** páginas con carga diferida; `jsPDF` se descarga recién al primer "Descargar PDF".
- **PostgreSQL** consume ~50–100 MB de RAM (SQL Server necesitaba ~2 GB).

## 9. Conceptos del dominio

- `kg CO₂ = consumo × factor de emisión`. El factor **se guarda en cada detalle** del registro: si el
  administrador lo cambia, el histórico no se altera.
- Totales en **toneladas de CO₂** y equivalente atmosférico: `1 ppm ≈ 7.800 millones de t CO₂`.
- **Alcances (GHG Protocol):** 1 = emisiones directas (gas, combustibles propios); 2 = energía adquirida
  (electricidad); 3 = otras indirectas (transporte de terceros, viajes, residuos, agua).
- **Períodos:** siempre el día 1 del mes; no se pueden cargar meses futuros.
- **Colaboraciones:** el titular invita a un usuario registrado a un domicilio, por un rango de períodos y
  ciertos tipos de emisión. Aceptada y vigente, el colaborador carga solo esos tipos; los registros suman
  al domicilio del titular y quedan marcados con quién los cargó.
- **Roles iniciales:** Administrador (panel, fuentes, usuarios, roles/permisos, catálogos, reportes globales);
  Individuo y Organización (dashboard, domicilios, emisiones, colaboraciones, reportes). Los permisos del
  sistema y los 3 roles iniciales no se pueden borrar.

## 10. Decisiones de diseño

| Decisión | Motivo |
|---|---|
| **PostgreSQL** en lugar de SQL Server | Usa ~20 veces menos RAM (el sistema corre en equipos de 4 GB), arranca en segundos, es libre y gratuito sin límites de edición, y tiene imágenes Docker livianas |
| Script SQL único e idempotente en lugar de migraciones de EF | El SQL es la fuente de verdad, se lee de corrido y corre en cada arranque sin pasos manuales |
| Nombres en MAYÚSCULA en el mapeo y minúscula en la base | El código se lee como el diagrama original; la base sigue la convención de PostgreSQL (sin comillas) |
| Arquitectura en 4 capas | Separación de responsabilidades pedida por la cátedra; facilita probar y cambiar cada parte |
| Permisos en el JWT | No hace falta consultar la base en cada petición para autorizar |
| Factor de emisión congelado en el detalle | Los reportes históricos no cambian cuando se actualizan factores |
| SMTP genérico (MailKit) en lugar del SDK de un proveedor | Se cambia de proveedor solo con configuración |
| Cola de emails en memoria | Simple y suficiente para el volumen esperado; una cola persistente (tabla o broker) sería el paso siguiente si el volumen crece |
| `REGISTROS` como cabecera + `DETALLE_REGISTRO` | Un mes tiene varios consumos; cambios respecto del esquema original detallados en el CHANGELOG (v1.0) |
