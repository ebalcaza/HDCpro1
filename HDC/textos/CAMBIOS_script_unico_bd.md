# Cambio: un único script de base de datos

## Lo pedido
1. Arreglar el error de `docker compose up -d --build`:
   `Container hdc-db-1 Error dependency db failed to start` (*container hdc-db-1 is unhealthy*).
2. Crear un script **definitivo** de base de datos para no tener varios scripts en el proyecto.

## Lo hecho
1. **Error de Docker**: el healthcheck del servicio `db` ahora espera más tiempo. Detalle en
   [`../CAMBIOS_healthcheck_db.md`](../CAMBIOS_healthcheck_db.md).
2. **Script único**: se unieron los tres scripts en `HDC_base_datos.sql`. Detalle a continuación.

## Antes
Había tres scripts en `backend/HDC.DAL/Scripts/`:

| Script                   | Contenido                    | Cuándo se ejecutaba             |
|--------------------------|------------------------------|---------------------------------|
| `01_esquema.sql`         | Tablas                       | Solo si la base estaba vacía    |
| `02_datos_iniciales.sql` | Roles, permisos y catálogos  | Solo si la base estaba vacía    |
| `03_indices.sql`         | Índices de rendimiento       | En cada arranque (idempotente)  |

## Ahora
Hay un solo archivo: **`backend/HDC.DAL/Scripts/HDC_base_datos.sql`**, dividido en tres secciones:

1. **Esquema**: cada `CREATE TABLE` va precedido de `IF OBJECT_ID(N'TABLA', N'U') IS NULL`.
2. **Datos iniciales**: cada bloque se inserta solo si su tabla está **vacía**
   (`IF NOT EXISTS (SELECT 1 FROM TABLA)`). Si el administrador después modifica o da de baja
   un dato, el script no lo vuelve a insertar.
3. **Índices**: cada uno con `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ...)`.
   Incluye `IX_REGISTROS_DOMICILIO_PERIODO`, que antes estaba en el script de esquema.

Como el script es **idempotente** (se puede ejecutar muchas veces y el resultado es el mismo):
- La API lo ejecuta en **cada arranque**. En una base vacía crea todo; en una existente solo
  agrega lo que falte (por ejemplo, una tabla o un índice nuevos).
- También se puede ejecutar a mano en SSMS o con sqlcmd (las instrucciones están en la cabecera del script).

## Archivos modificados
- `backend/HDC.DAL/Scripts/HDC_base_datos.sql`: **nuevo**, reemplaza a los tres anteriores (borrados).
- `backend/HDC.DAL/InicializadorBaseDatos.cs`: ya no consulta si existe la tabla `ROL` ni maneja dos
  listas de scripts. Ejecuta `HDC_base_datos.sql` dentro de una transacción (si un lote falla, no
  queda nada a medio crear), con un `CommandTimeout` de 600 s.
- `backend/HDC.BLL/Comun/Permisos.cs`, `backend/HDC.DAL/HdcDbContext.cs`,
  `backend/HDC.Entity/Validaciones/Reglas.cs`, `README.md`: comentarios actualizados para que
  apunten al nuevo script.

El `.csproj` no cambia: ya embebía `Scripts\*.sql`.

## Verificación
- `dotnet build` de `HDC.DAL`: compila sin errores.
- Se creó una base temporal `HDC_Prueba` en el contenedor `db` y se ejecutó el script **dos veces seguidas**:
  - 1ª ejecución: crea las 20 tablas y carga 3 roles, 12 permisos, 19 asignaciones rol-permiso,
    3 alcances, 5 unidades, 8 tipos de emisión y 15 fuentes.
  - 2ª ejecución: termina sin errores y no duplica ningún dato.
  - Después se borró la base de prueba.
- Pendiente: probar el arranque completo de la API con `docker compose up -d --build`.

## Cómo agregar cambios a la base en el futuro
Agregarlos al final de la sección correspondiente de `HDC_base_datos.sql`, siempre con su
`IF NOT EXISTS` / `IF OBJECT_ID(...) IS NULL`. Para una columna nueva en una tabla existente:

```sql
IF COL_LENGTH('DOMICILIO', 'NUEVA_COLUMNA') IS NULL
    ALTER TABLE DOMICILIO ADD NUEVA_COLUMNA NVARCHAR(50) NULL;
GO
```
