# Huella de Carbono (HDC)

Aplicación web para que personas y organizaciones calculen su **huella de carbono** a partir de sus
consumos mensuales (gas, electricidad, combustible, transporte, residuos, agua…), vean su evolución y
descarguen reportes.

**Versión 2.1** · React 19 + .NET 9 + PostgreSQL 17 · Docker Compose

## Documentación

| Documento | Para quién | Contenido |
|---|---|---|
| [Manual de usuario](docs/MANUAL_USUARIO.md) | Usuarios finales | Cómo usar cada pantalla, recuperar la contraseña, preguntas frecuentes |
| [Guía del desarrollador](docs/GUIA_DESARROLLADOR.md) | Desarrolladores | Levantar el proyecto, variables, datos de prueba, desarrollo local, producción, problemas frecuentes |
| [Documentación técnica](docs/DOCUMENTACION_TECNICA.md) | Equipo técnico | Arquitectura, tecnologías, modelo de datos, API, seguridad, decisiones de diseño |
| [Historial de cambios](docs/CHANGELOG.md) | Todos | Qué cambió en cada versión, por qué y en qué archivos |

## Inicio rápido

Requisito: **Docker Desktop**. Desde esta carpeta:

```bash
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
# editá .env: como mínimo DB_CONTRASENA, JWT_CLAVE y ADMIN_CONTRASENA
docker compose up -d --build
```

| Qué | Dónde |
|---|---|
| Aplicación | http://localhost:8081 |
| API (Swagger) | http://localhost:5000/swagger |
| Emails de prueba (Mailpit) | http://localhost:8025 |

Cuentas para probar (con `DATOS_PRUEBA=true`, se crean ~1 minuto después del primer arranque):

| Email | Contraseña | Tipo |
|---|---|---|
| `admin@hdc.com` (o `ADMIN_EMAIL`) | `ADMIN_CONTRASENA` del `.env` | Administrador |
| `maria.gomez@prueba.com` | `Prueba2026` | Individuo |
| `juan.perez@prueba.com` | `Prueba2026` | Individuo |
| `contacto@ecosur.prueba.com` | `Prueba2026` | Organización |

Más detalle en la [Guía del desarrollador](docs/GUIA_DESARROLLADOR.md).

## Estructura

```
HDC/
├─ README.md                 este archivo
├─ docs/                     toda la documentación (ver tabla de arriba)
├─ docker-compose.yml        db (PostgreSQL) + mailpit + api (.NET) + frontend (nginx)
├─ .env.example              variables de entorno (copiar a .env)
├─ backend/                  API .NET 9 en 4 capas: HDC.Entity, HDC.DAL, HDC.BLL, HDC.API
├─ frontend/                 SPA React + Vite
└─ textos/                   consignas y notas de trabajo
```
