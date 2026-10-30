# Cambios: se quita todo lo de "vehículos"

La sección de vehículos ya se había sacado de la aplicación (ver `fixVarios1.txt`).
Lo único que seguía en el proyecto era la tabla `VEHICULO` del script de base de datos
y su mención en la documentación.

## Archivos modificados

| Archivo | Cambio | Por qué |
|---------|--------|---------|
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` | Se reemplazó el `CREATE TABLE VEHICULO` por `IF OBJECT_ID(N'VEHICULO', N'U') IS NOT NULL DROP TABLE VEHICULO;` | La API ejecuta este script en cada arranque. Así una base nueva no crea la tabla, y una base existente la borra |
| `backend/HDC.DAL/Scripts/HDC_base_datos.sql` (comentario inicial) | Se quitó `VEHICULO` de la lista del modelo y se anotó que se elimina | Mantener actualizadas las notas del script |
| `README.md` y `textos/README.md` | Se quitó `VEHICULO` de la tabla de cambios del modelo | La tabla ya no existe |

## Qué NO se tocó (a propósito)

- **Fuentes de emisión "Nafta / Gasoil / GNC - vehículo propio"** en los datos iniciales y en
  `MANUAL_USUARIO.md`: son factores de emisión de **combustible** y se usan para cargar consumos.
  No forman parte de la sección de vehículos.
- **`textos/instruccionesClaude.txt` y `textos/fixVarios1.txt`**: son las consignas originales
  del proyecto y se dejaron como registro histórico.

## Efecto en bases existentes

En el próximo arranque de la API se borra la tabla `VEHICULO` junto con los datos que tenga.
No tiene relaciones hacia otras tablas: solo dependía de `USUARIO`. Por eso borrarla no afecta al resto.
