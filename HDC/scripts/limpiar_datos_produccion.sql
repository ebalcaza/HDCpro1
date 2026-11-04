/* =====================================================================
   LIMPIEZA DE DATOS PARA PASAR A PRODUCCIÓN — Huella de Carbono (HDC)
   ---------------------------------------------------------------------
   ⚠️ BORRA DE FORMA DEFINITIVA TODOS LOS DATOS DE USO DEL SISTEMA.
   Cómo usarlo: docs/GUIA_DESARROLLADOR.md, sección 9.

   SE BORRA: usuarios que no sean administradores (con su individuo u
   organización), domicilios, registros y sus detalles, colaboraciones,
   convocatorias, reportes y pedidos de recuperación de contraseña.
   También lo que hubiera cargado el propio administrador.

   SE CONSERVA: las cuentas con rol Administrador, los roles y permisos,
   los catálogos (alcances, unidades, tipos y fuentes de emisión) y la
   geografía. El sistema los necesita para funcionar.

   Este archivo está fuera de backend/HDC.DAL/Scripts a propósito: todo lo
   que hay en esa carpeta se embebe en la DLL de la API.

   Protecciones:
   - Todo corre en una transacción: si algo falla, no se borra nada.
   - No hace nada sin la confirmación explícita (hdc.confirmar_borrado).
   - Se cancela si no queda ningún administrador activo para entrar.
   ===================================================================== */

BEGIN;

/* Si esta comprobación falla, la transacción queda abortada: PostgreSQL rechaza
   las sentencias siguientes y el COMMIT final se convierte en ROLLBACK. Así el
   script no borra nada aunque el cliente siga ejecutando después del error. */
DO $$
BEGIN
    -- La confirmación debe ser el nombre de la base: evita borrar por error en otra
    IF COALESCE(current_setting('hdc.confirmar_borrado', TRUE), '') <> current_database() THEN
        RAISE EXCEPTION 'Sin confirmar: no se borró nada. Indicá hdc.confirmar_borrado = %', current_database();
    END IF;

    -- Sin un administrador activo nadie podría entrar a administrar el sistema
    IF NOT EXISTS (SELECT 1
                   FROM usuario u JOIN rol r USING (id_rol)
                   WHERE r.nombre = 'Administrador' AND u.fecha_baja IS NULL) THEN
        RAISE EXCEPTION 'No hay ningún administrador activo: no se borró nada.';
    END IF;
END $$;

/* El orden importa: estas tablas apuntan a usuario o a domicilio sin borrado en
   cascada, así que se vacían antes que ellos. */
DELETE FROM registros;                 -- en cascada: detalle_registro
DELETE FROM carga_traslado;            -- apunta a usuario; en cascada: carga_traslado_medio
DELETE FROM colaboracion;              -- en cascada: colaboracion_tipo_emision
DELETE FROM convocatoria;              -- en cascada: convocatoria_tipo_emision y convocatoria_rol
DELETE FROM domicilio;
DELETE FROM reporte;
DELETE FROM recuperacion_contrasena;   -- incluye los links pendientes del administrador

-- En cascada: individuo y organizacion
DELETE FROM usuario
WHERE id_rol NOT IN (SELECT id_rol FROM rol WHERE nombre = 'Administrador');

-- Muestra qué quedó, para revisarlo en la salida de la ejecución
SELECT u.email AS cuentas_conservadas, r.nombre AS rol
FROM usuario u JOIN rol r USING (id_rol)
ORDER BY u.email;

COMMIT;
