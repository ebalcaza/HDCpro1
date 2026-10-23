# Mejoras de rendimiento – HDC

Fecha: 21/10/2026

**Objetivo:** que la aplicación siga siendo rápida cuando haya muchos registros, usuarios y meses de datos.

**Idea general:** que la base de datos haga el trabajo pesado (filtrar, ordenar, contar y sumar) y que por la red y en el navegador viaje solo lo que se va a mostrar.

Los cambios se hicieron en este orden:

| # | Mejora | Capa | Impacto |
|---|--------|------|---------|
| 1 | Paginado en el servidor (Mis emisiones y Usuarios) | Backend + Frontend | Alto |
| 2 | Totales del Dashboard calculados por SQL | Backend | Alto |
| 3 | Índices en la base de datos | Base de datos | Alto, con poco esfuerzo |
| 4 | Cache de catálogos y geografía | Backend + Frontend | Medio |
| 5 | Carga diferida del generador de PDF | Frontend | Medio (primera carga) |
| 6 | Espera en el buscador (debounce), compresión y proyección de usuarios | Backend + Frontend | Medio |

---

## 1. Paginado en el servidor

### Problema
"Mis emisiones" y "Usuarios" (admin) pedían **la lista completa** a la API: cada registro venía con su domicilio, ciudad, provincia, usuario y todos sus consumos. Recién en el navegador se cortaba en páginas de 10. Con miles de registros eso significa:
- consultas pesadas en SQL Server,
- respuestas de varios MB,
- más memoria y más tiempo de dibujo en el navegador.

### Solución
La API recibe qué página se quiere y devuelve solo esa página más el total de filas.

```
GET /api/registros?pagina=2&tamanio=20&orden=totalKgCo2&asc=false&busqueda=gas&idDomicilio=3&desde=2026-01-01
→ { items: [...20 registros...], total: 347, totalKgCo2: 15230.5 }

GET /api/usuarios?pagina=1&tamanio=10&estado=activos&idRol=2&busqueda=juan
→ { items: [...], total: 58 }
```

### Archivos

**Backend**
- `HDC.Entity/Dtos/PaginacionDtos.cs` (nuevo):
  - `ConsultaPagina` con los parámetros comunes. El tamaño solo puede ser 10, 20, 50 o 100, para que nadie pida 100.000 filas de una vez.
  - `PaginaDto<T>`, `FiltroRegistros`, `FiltroUsuarios` y `ResumenUsuariosDto`.
- `HDC.DAL/Repositorios/ExtensionesConsulta.cs` (nuevo): `OrdenarPor(...)` y `Paginar(...)`, que se traducen a `ORDER BY ... OFFSET ... FETCH` en SQL.
- `RegistroRepositorio.PaginarVisiblesAsync` trabaja **en dos pasos**:
  1. Con una consulta liviana (sin `Include`) filtra, cuenta (`COUNT`), suma (`SUM`) y obtiene solo los ids de la página.
  2. Carga completos únicamente esos registros (como mucho 100).
- `RegistroRepositorio.DomiciliosVisiblesAsync` y el endpoint `GET /api/registros/domicilios` llenan el combo "Domicilio". Antes ese combo salía de la lista completa, que ya no se descarga.
- `UsuarioRepositorio.PaginarAsync`, junto con `GET /api/usuarios/resumen` para el panel admin y el filtro "Tipo".

**Frontend**
- `components/comunes/TablaPaginada.jsx` ahora tiene dos modos:
  - **Local** (por defecto): igual que antes. Sirve para listas chicas: catálogos, roles y las tablas de los gráficos.
  - **Servidor** (cuando se le pasa `onConsulta`): la tabla avisa qué página, orden y búsqueda quiere, y la página padre la pide a la API.
- `consultaAParams(...)` convierte el estado de la tabla en los parámetros de la API.
- `claveFiltros`: cuando cambian los filtros externos (domicilio o fechas), la tabla vuelve a la página 1.
- Mientras llega la página nueva, la anterior se ve atenuada (no aparece el esqueleto de carga cada vez).
- `pages/usuario/MisEmisiones.jsx` y `pages/admin/Usuarios.jsx` usan el modo servidor.
- `pages/admin/AdminPanel.jsx` usa `/usuarios/resumen` (dos `COUNT`) en lugar de bajar todos los usuarios para contarlos.

### Qué quedó con paginado local, y por qué
- El **detalle del reporte** (Reportes → "Detalle de registros"): esas filas también se usan para armar el PDF completo, así que tienen que llegar todas.
- Los **catálogos y roles**: son listas cortas.

### Diferencia de comportamiento
- El buscador de Mis emisiones busca por calle, ciudad, quién cargó, alcance o nombre de fuente. Ya no busca por el texto del mes (por ejemplo, "marzo"). Para eso están los filtros Desde y Hasta.
- Con la configuración por defecto de SQL Server, la búsqueda no distingue mayúsculas, pero sí tildes.

---

## 2. Dashboard calculado por SQL

### Problema
`DashboardAsync` traía **cada consumo** del usuario como una fila (con unas 25 columnas) y después sumaba en C# por alcance, tipo, fuente, domicilio, mes y colaborador. Con 3 años de datos eso ya son miles de filas en cada visita al Dashboard.

### Solución
`RegistroRepositorio.ObtenerAgregadosAsync` hace las sumas con `GROUP BY` en SQL Server:

| Consulta | Devuelve | Se usa para |
|----------|----------|-------------|
| kg por mes y alcance | ~ meses × alcances filas | total, mes actual y anterior, dona, evolución |
| kg por tipo de emisión | una fila por tipo | tabla de la dona |
| kg y consumo por fuente | una fila por fuente | "Fuentes que más emiten" |
| kg por domicilio | una fila por domicilio | por domicilio |
| kg por colaborador | una fila por colaborador | "Aportes de colaboradores" |
| `COUNT(DISTINCT)` | 2 números | lectura rápida |

En `EstadisticaServicio.DashboardAsync` ahora solo se calculan los porcentajes y la serie mensual a partir de esas pocas filas. Se agregaron los records `SumaPeriodoAlcance`, `SumaGrupo` y `AgregadosEmisiones` en `EmisionDtos.cs`.

**Reportes:** siguen trayendo las filas de detalle, porque las necesitan para la tabla y el PDF. Como esos datos ya están en memoria, los agrupa el servicio sin volver a consultar la base.

El filtrado se separó en `Filtrar(...)`, que comparten el Dashboard y los reportes. La serie mensual (`Evolucion`) quedó en una sola función que usan los dos.

---

## 3. Índices en la base de datos

### Problema
SQL Server **no crea índices automáticamente para las claves foráneas**: solo los crea para `PRIMARY KEY` y `UNIQUE`. Por eso cada `JOIN` o `WHERE` por esas columnas recorría la tabla completa. Había un solo índice (`IX_REGISTROS_DOMICILIO_PERIODO`).

### Solución
Nuevo script `HDC.DAL/Scripts/03_indices.sql`:

| Índice | Para qué |
|--------|----------|
| `IX_DETALLE_REGISTRO_REGISTRO` (+INCLUDE fuente, consumo, kg) | abrir registros y todas las sumas |
| `IX_DETALLE_REGISTRO_FUENTE` | agrupar y filtrar por fuente |
| `IX_DOMICILIO_USUARIO` | domicilios de un usuario, visibilidad |
| `IX_DOMICILIO_CIUDAD` | filtros geográficos |
| `IX_REGISTROS_USUARIO_CARGA` | registros cargados como colaborador |
| `IX_COLABORACION_DOMICILIO_ESTADO` | validar colaboraciones aceptadas |
| `IX_COLABORACION_COLABORADOR` | colaboraciones recibidas |
| `IX_USUARIO_ROL` | filtro por tipo de usuario |
| `IX_REPORTE_USUARIO` | reportes guardados |

`INCLUDE (...)` arma un **índice de cobertura**: si la consulta solo necesita esas columnas, se resuelve leyendo el índice, sin ir a la tabla.

Cada índice se crea con `IF NOT EXISTS`, así que el script se puede ejecutar muchas veces. `InicializadorBaseDatos.cs` ahora tiene una lista `ScriptsSiempre` que corre **en cada arranque**. Así los índices también llegan a las bases que ya existían, sin borrarlas.

---

## 4. Cache de catálogos y geografía

### Problema
Fuentes, alcances, tipos, unidades y la geografía casi nunca cambian. Sin embargo, se consultaban a la base cada vez que alguien abría el formulario de registro, Reportes, Colaboraciones o el registro de usuarios.

### Solución en el backend
`HDC.BLL/Comun/CacheCatalogos.cs` (nuevo, Singleton con `IMemoryCache`):
- La primera consulta va a la base. Las siguientes salen de memoria.
- Hay dos grupos: `catalogos` y `geo`.
- Cada alta, edición, baja o reactivación de un catálogo llama a `Invalidar("catalogos")`. La sincronización con georef llama a `Invalidar("geo")`.
- Por seguridad, cada entrada vence sola a la hora.
- Para borrar todo un grupo a la vez se usa un `CancellationToken` por grupo, porque `IMemoryCache` no permite borrar por prefijo.

Se aplicó en `CatalogoServicio`, `FuenteEmisionServicio` y `SemillaServicio`, y se registró en `DependencyInjection.cs`. No hizo falta ningún paquete NuGet nuevo: EF Core ya trae `Microsoft.Extensions.Caching.Memory`.

### Solución en el frontend
En `api/servicios.js`:
- Se guarda la **promesa** de cada GET de catálogos y geografía mientras dura la sesión. Si dos pantallas piden lo mismo, viaja una sola petición.
- Crear, editar, eliminar o reactivar desde este navegador vacía la cache de catálogos. Sincronizar georef vacía la de geografía.
- Al recargar la página (F5) la cache arranca vacía.

---

## 5. Carga diferida del PDF

### Problema
`jspdf` + `jspdf-autotable` pesan unos 430 kB (140 kB comprimidos). Se descargaban apenas se entraba a Mis emisiones, Detalle de registro o Reportes, aunque el usuario nunca exportara nada.

### Solución
- Nuevo `utils/pdfDiferido.js`, con `descargarPdfRegistro` y `descargarPdfReporte`. Usan `import('./pdf')` dinámico, así que Vite deja `pdf.js` en un archivo aparte que se descarga **recién al primer click en "Descargar PDF"**.
- `MisEmisiones.jsx`, `RegistroDetalle.jsx` y `Reportes.jsx` usan estas funciones y muestran un error si la descarga falla.
- Se eliminó `utils/pdf2.js`: era una copia vieja de `pdf.js` que no se importaba en ningún lado. Sigue disponible en el historial de git si hace falta.

---

## 6. Otros ajustes

- **Debounce del buscador** (`TablaPaginada.jsx`): la búsqueda se aplica 250 ms después de la última tecla (`ESPERA_BUSQUEDA`). En el modo servidor eso evita una llamada a la API por cada letra; en el modo local, filtrar en cada tecla.
- **Una sola consulta por cambio:** el estado de la tabla (página, tamaño, orden y texto) vive en un único objeto. Por ejemplo, cambiar el tamaño también vuelve a la página 1 en el mismo paso, sin disparar dos pedidos.
- **Compresión de respuestas** (`Program.cs`): `AddResponseCompression` con Brotli y gzip en nivel `Fastest`. El JSON se reduce entre 70 % y 90 %. En producción nginx ya comprimía; esto cubre el desarrollo y el acceso directo a la API.
- **Listado de usuarios proyectado:** antes se traían todos los domicilios de cada usuario (`Include`) solo para contarlos y sumarlos. Ahora la consulta hace `COUNT` y `SUM` en SQL y devuelve una fila liviana por usuario.

---

## Cómo se verificó

- `dotnet build` del backend y `vite build` del frontend sin errores.
- Una prueba descartable contra **SQL Server LocalDB**, con una base temporal que después se borró:
  - Las 24 variantes de consulta nuevas (cada orden, filtro y búsqueda) **se traducen a SQL y se ejecutan**.
  - `03_indices.sql` se ejecutó dos veces seguidas sin error y creó los 9 índices.
  - Se cargaron 60 registros de prueba. Los resultados nuevos (SQL) se compararon con el cálculo anterior (en memoria) en 20 puntos: total, conteos, sumas por tipo, fuente, domicilio y colaborador, paginado, orden, filtros, búsqueda y visibilidad del colaborador. **Todos coinciden.**
- **No se probó la aplicación completa en el navegador.** Conviene revisar Mis emisiones, Usuarios, Dashboard y la descarga de PDF.

## Notas

- **Cambio de contrato de la API:**
  - `GET /api/registros` y `GET /api/usuarios` ahora devuelven `{ items, total }` en lugar de un array.
  - Se quitó el endpoint `PUT /api/usuarios/{id}/rol` (pedido en fix3).
  - Si algún otro cliente usaba esos endpoints, hay que adaptarlo.
- **Equipo con 4 GB de RAM:** para desarrollar conviene `npm run dev` + `dotnet run` contra LocalDB, en lugar de levantar los contenedores de Docker.
