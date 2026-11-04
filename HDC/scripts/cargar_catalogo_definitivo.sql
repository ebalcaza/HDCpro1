/* =====================================================================
   CATÁLOGO DEFINITIVO DE FUENTES DE EMISIÓN — Huella de Carbono (HDC)
   ---------------------------------------------------------------------
   Para una base existente. Una base nueva ya lo recibe del script de inicio.
   Cómo usarlo: docs/GUIA_DESARROLLADOR.md, sección 9.

   - Actualiza tipos y unidades, y carga o actualiza las fuentes por nombre.
   - Las fuentes que ya no están en la lista se borran si nunca se usaron.
     Si tienen consumos cargados, se dan de baja para conservar el historial.
   - No modifica los consumos ya cargados: cada uno conserva su factor.
   - Se puede volver a ejecutar. Corre en una transacción: si algo falla, no cambia nada.
   - Pisa los cambios hechos desde el panel en las fuentes de esta lista.
   ===================================================================== */

BEGIN;

UPDATE alcance SET descripcion = 'Otras emisiones indirectas (movilidad, viajes, residuos, agua, papel e insumos)' WHERE id_alcance = 3;

INSERT INTO unidad_medida (nombre) VALUES ('resma'), ('unidad') ON CONFLICT (nombre) DO NOTHING;

/* Los tipos conservan su id: las convocatorias y colaboraciones siguen apuntando a ellos */
UPDATE tipo_emision t SET nombre = v.nuevo
FROM (VALUES
 ('Combustión estacionaria', 'Combustión fija'),
 ('Combustión móvil propia', 'Combustión móvil'),
 ('Emisiones fugitivas', 'Fugas de gases refrigerantes'),
 ('Electricidad adquirida', 'Electricidad y energías adquiridas'),
 ('Transporte de colaboradores', 'Traslado de colaboradores'),
 ('Viajes', 'Viajes institucionales')
) AS v(viejo, nuevo)
WHERE t.nombre = v.viejo;

INSERT INTO tipo_emision (id_alcance, nombre)
SELECT 3, 'Papel e insumos' WHERE NOT EXISTS (SELECT 1 FROM tipo_emision WHERE nombre = 'Papel e insumos');

UPDATE tipo_emision SET modo_carga = CASE nombre
    WHEN 'Combustión móvil' THEN 'VEHICULO'
    WHEN 'Traslado de colaboradores' THEN 'TRASLADO'
    WHEN 'Viajes institucionales' THEN 'VIAJE'
    ELSE 'CONSUMO' END;

CREATE TEMP TABLE catalogo_definitivo (tipo TEXT, unidad TEXT, nombre TEXT, factor NUMERIC(18,6), descripcion TEXT) ON COMMIT DROP;
INSERT INTO catalogo_definitivo VALUES
 ('Combustión fija', 'm3', 'Gas natural de red', 1.956800::NUMERIC(18,6), 'Gas de red para calefacción, agua caliente y procesos. El consumo en m³ figura en la factura del proveedor. Se carga por mes. Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5.'),
 ('Combustión fija', 'kg', 'Gas envasado (GLP) en kg', 2.908700::NUMERIC(18,6), 'Garrafas o tubos, solo si corresponde. Usá esta opción si la compra está en kilos (facturas o registros de compra). Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5.'),
 ('Combustión fija', 'L', 'Gas envasado (GLP) en litros', 1.672400::NUMERIC(18,6), 'Gas envasado, solo si corresponde. Usá esta opción si la compra está en litros (facturas o registros de compra). Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5.'),
 ('Combustión fija', 'L', 'Gasoil para grupos electrógenos', 2.546000::NUMERIC(18,6), 'Litros de gasoil consumidos por grupos electrógenos en el mes, según el registro de mantenimiento o las compras. Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5. Gasoil grado 2, instalaciones fijas.'),
 ('Combustión móvil', 'L', 'Nafta para vehículos', 2.020900::NUMERIC(18,6), 'Vehículos propios o bajo control de la organización. Litros del mes según tarjeta de combustible o comprobantes; los km son opcionales. Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5. Nafta grado 2 con 12 % de bioetanol, sin CO₂ biogénico.'),
 ('Combustión móvil', 'L', 'Gasoil para vehículos', 2.379000::NUMERIC(18,6), 'Vehículos propios o bajo control de la organización. Litros del mes según tarjeta de combustible o comprobantes; los km son opcionales. Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5. Gasoil grado 2 con 7,5 % de biodiésel, sin CO₂ biogénico.'),
 ('Fugas de gases refrigerantes', 'kg', 'R-410A (aire y climatización central)', 1923.500000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR5 (SNIGEI-AR 2025).'),
 ('Fugas de gases refrigerantes', 'kg', 'R-32 (aire y climatización central)', 677.000000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR5 (SNIGEI-AR 2025).'),
 ('Fugas de gases refrigerantes', 'kg', 'R-22 (aire y climatización central)', 1760.000000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR5 (SNIGEI-AR 2025).'),
 ('Fugas de gases refrigerantes', 'kg', 'R-290 / propano (aire y climatización central)', 0.020000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR6 (SNIGEI-AR 2025; no figura en AR5).'),
 ('Fugas de gases refrigerantes', 'kg', 'R-134a (heladeras y freezers)', 1300.000000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR5 (SNIGEI-AR 2025).'),
 ('Fugas de gases refrigerantes', 'kg', 'R-600a / isobutano (heladeras y freezers)', 0.000000::NUMERIC(18,6), 'Kilos recargados, según la empresa de mantenimiento. Se carga cuando ocurre. Factor: PCG a 100 años, IPCC AR5 (SNIGEI-AR 2025).'),
 ('Electricidad y energías adquiridas', 'kWh', 'Electricidad de la red', 0.286000::NUMERIC(18,6), 'Consumo en kWh según la factura del proveedor eléctrico. Se carga por mes. Factor: SNIGEI-AR 2025 (INI 2022), CO₂ fósil + CH₄ + N₂O con PCG AR5. Año 2022.'),
 ('Electricidad y energías adquiridas', 'kWh', 'Electricidad de energías renovables', 0.000000::NUMERIC(18,6), 'Energía renovable adquirida y verificada, si corresponde. Consumo en kWh según la factura. Factor 0: enfoque basado en el mercado (GHG Protocol Scope 2).'),
 ('Traslado de colaboradores', 'km', 'Automóvil a nafta', 0.164300::NUMERIC(18,6), 'Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: DEFRA / GHG Protocol. Auto mediano, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Automóvil a gasoil', 0.161200::NUMERIC(18,6), 'Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: DEFRA / GHG Protocol. Auto mediano, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Motocicleta a nafta', 0.111400::NUMERIC(18,6), 'Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: promedio de cilindrada mixta, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Colectivo', 0.094200::NUMERIC(18,6), 'Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: transporte urbano pesado a gasoil, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Tren', 0.037800::NUMERIC(18,6), 'Tren eléctrico. Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: red ferroviaria nacional, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Subte', 0.026500::NUMERIC(18,6), 'Subte eléctrico. Se calcula con la distancia de ida y vuelta y los días de asistencia. Factor: tracción eléctrica metropolitana, por pasajero-km.'),
 ('Traslado de colaboradores', 'km', 'Bicicleta', 0.000000::NUMERIC(18,6), 'Traslado sin emisiones. Se registra igual para conocer cómo viaja la comunidad.'),
 ('Traslado de colaboradores', 'km', 'Caminata', 0.000000::NUMERIC(18,6), 'Traslado sin emisiones. Se registra igual para conocer cómo viaja la comunidad.'),
 ('Viajes institucionales', 'km', 'Viaje en automóvil a nafta', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Nafta para vehículos × 0,0813 L por km.'),
 ('Viajes institucionales', 'km', 'Viaje en automóvil a gasoil', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Gasoil para vehículos × 0,0678 L por km.'),
 ('Viajes institucionales', 'km', 'Viaje en motocicleta a nafta', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Nafta para vehículos × 0,0551 L por km.'),
 ('Viajes institucionales', 'km', 'Viaje en colectivo a gasoil', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Gasoil para vehículos × 0,0396 L por pasajero-km.'),
 ('Viajes institucionales', 'km', 'Viaje en tren eléctrico', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Electricidad de la red × 0,1322 kWh por pasajero-km.'),
 ('Viajes institucionales', 'km', 'Viaje en subte eléctrico', 0.000000::NUMERIC(18,6), 'Distancia y fecha según rendiciones o registros administrativos. Factor: Electricidad de la red × 0,0927 kWh por pasajero-km.'),
 ('Viajes institucionales', 'km', 'Viaje en avión', 0.000000::NUMERIC(18,6), 'Solo se guarda como dato: no suma emisiones. Distancia y fecha según rendiciones o registros administrativos.'),
 ('Residuos', 'kg', 'Residuos sólidos urbanos', 1.176000::NUMERIC(18,6), 'Kilos generados, según la empresa recolectora o una estimación interna. Factor: SNIGEI-AR 2025, relleno sanitario con captura de metano (CEAMSE), 0,042 kg CH₄/kg con PCG AR5.'),
 ('Residuos', 'kg', 'Papel y cartón reciclado', 0.004700::NUMERIC(18,6), 'Kilos reciclados, según registros internos. Factor: DEFRA / GHG Protocol. Disposición por reciclaje (4,65 kg CO₂e/t).'),
 ('Residuos', 'kg', 'Plásticos reciclados', 0.004700::NUMERIC(18,6), 'Kilos reciclados, según registros internos. Factor: DEFRA / GHG Protocol. Disposición por reciclaje (4,65 kg CO₂e/t).'),
 ('Residuos', 'kg', 'Vidrio reciclado', 0.004700::NUMERIC(18,6), 'Kilos reciclados, según registros internos. Factor: DEFRA / GHG Protocol. Disposición por reciclaje (4,65 kg CO₂e/t).'),
 ('Residuos', 'kg', 'Residuos electrónicos (RAEE)', 0.004700::NUMERIC(18,6), 'Kilos gestionados, según actas de disposición. Factor: DEFRA / GHG Protocol. Disposición por reciclaje (4,65 kg CO₂e/t).'),
 ('Residuos', 'kg', 'Residuos peligrosos de laboratorio', 0.825000::NUMERIC(18,6), 'Kilos gestionados, según los manifiestos. Factor: SNIGEI-AR 2025, incineración de residuos peligrosos (0,825 t CO₂/t).'),
 ('Residuos', 'L', 'Aceites usados', 0.019200::NUMERIC(18,6), 'Litros gestionados, si corresponde, según el registro de disposición. Factor: DEFRA / GHG Protocol. Tratamiento especializado, densidad 0,9 kg/L.'),
 ('Agua', 'm3', 'Agua potable', 0.191300::NUMERIC(18,6), 'Consumo en m³ según la factura del proveedor. Se carga por mes. Factor: DEFRA / GHG Protocol. Tratamiento y bombeo de agua de red.'),
 ('Papel e insumos', 'resma', 'Papel A4 virgen', 2.288300::NUMERIC(18,6), 'Resmas adquiridas según compras. Resma de 80 g y 500 hojas: 2,49 kg × 0,919 kg CO₂e/kg (DEFRA / GHG Protocol).'),
 ('Papel e insumos', 'resma', 'Papel A3 virgen', 4.585800::NUMERIC(18,6), 'Resmas adquiridas según compras. Resma de 80 g y 500 hojas: 4,99 kg × 0,919 kg CO₂e/kg (DEFRA / GHG Protocol).'),
 ('Papel e insumos', 'resma', 'Papel A4 reciclado', 1.474100::NUMERIC(18,6), 'Resmas adquiridas según compras. Resma de 80 g y 500 hojas: 2,49 kg × 0,592 kg CO₂e/kg (DEFRA / GHG Protocol).'),
 ('Papel e insumos', 'resma', 'Papel A3 reciclado', 2.954100::NUMERIC(18,6), 'Resmas adquiridas según compras. Resma de 80 g y 500 hojas: 4,99 kg × 0,592 kg CO₂e/kg (DEFRA / GHG Protocol).'),
 ('Papel e insumos', 'unidad', 'Tóner para impresoras', 4.800000::NUMERIC(18,6), 'Unidades adquiridas según compras. Factor: tóner láser nuevo, ciclo de vida hasta la entrega.'),
 ('Papel e insumos', 'unidad', 'Tóner remanufacturado', 1.250000::NUMERIC(18,6), 'Unidades adquiridas según compras. Factor: tóner remanufacturado, ciclo de vida hasta la entrega.'),
 ('Papel e insumos', 'unidad', 'Cartuchos de tinta', 3.200000::NUMERIC(18,6), 'Unidades adquiridas según compras. Factor: promedio de ciclo de vida de cartuchos inkjet.'),
 ('Papel e insumos', 'kg', 'Insumos de oficina (opcional)', 1.210000::NUMERIC(18,6), 'Kilos adquiridos según compras (carpetas, abrochadoras, lapiceras, etc.). Factor ponderado por peso para artículos de oficina.');

/* Si falta un tipo o una unidad, se cancela todo */
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM catalogo_definitivo c
               LEFT JOIN tipo_emision t ON t.nombre = c.tipo
               LEFT JOIN unidad_medida u ON u.nombre = c.unidad
               WHERE t.id_tipo_emision IS NULL OR u.id_unidad_medida IS NULL) THEN
        RAISE EXCEPTION 'Falta un tipo o una unidad del catálogo: no se cambió nada.';
    END IF;
END $$;

/* Fuentes que siguen: se actualizan y se reactivan */
UPDATE fuente_emision f
SET id_unidad_medida = u.id_unidad_medida, id_tipo_emision = t.id_tipo_emision,
    valor_factor_emision = c.factor, descripcion = c.descripcion,
    id_fuente_base = NULL, consumo_por_km = NULL,
    fecha_baja = NULL, fecha_modificacion = LOCALTIMESTAMP(0)
FROM catalogo_definitivo c
JOIN tipo_emision t ON t.nombre = c.tipo
JOIN unidad_medida u ON u.nombre = c.unidad
WHERE f.nombre = c.nombre;

/* Fuentes nuevas */
INSERT INTO fuente_emision (id_unidad_medida, id_tipo_emision, nombre, valor_factor_emision, descripcion)
SELECT u.id_unidad_medida, t.id_tipo_emision, c.nombre, c.factor, c.descripcion
FROM catalogo_definitivo c
JOIN tipo_emision t ON t.nombre = c.tipo
JOIN unidad_medida u ON u.nombre = c.unidad
WHERE NOT EXISTS (SELECT 1 FROM fuente_emision f WHERE f.nombre = c.nombre);

/* Viajes institucionales (v2.9): el factor por km sale de los combustibles y la electricidad de esta misma lista */
UPDATE fuente_emision f
SET id_fuente_base = b.id_fuente_emision, consumo_por_km = v.consumo,
    valor_factor_emision = ROUND(b.valor_factor_emision * v.consumo, 6)
FROM (VALUES
 ('Viaje en automóvil a nafta', 'Nafta para vehículos', 0.0813::NUMERIC(18,6)),
 ('Viaje en automóvil a gasoil', 'Gasoil para vehículos', 0.0678::NUMERIC(18,6)),
 ('Viaje en motocicleta a nafta', 'Nafta para vehículos', 0.0551::NUMERIC(18,6)),
 ('Viaje en colectivo a gasoil', 'Gasoil para vehículos', 0.0396::NUMERIC(18,6)),
 ('Viaje en tren eléctrico', 'Electricidad de la red', 0.1322::NUMERIC(18,6)),
 ('Viaje en subte eléctrico', 'Electricidad de la red', 0.0927::NUMERIC(18,6))
) AS v(nombre, base, consumo)
JOIN fuente_emision b ON b.nombre = v.base
WHERE f.nombre = v.nombre;

/* Fuentes viejas: se dan de baja si tienen consumos y, si no, se borran */
UPDATE fuente_emision f SET fecha_baja = LOCALTIMESTAMP(0)
WHERE f.fecha_baja IS NULL
  AND NOT EXISTS (SELECT 1 FROM catalogo_definitivo c WHERE c.nombre = f.nombre)
  AND EXISTS (SELECT 1 FROM detalle_registro d WHERE d.id_fuente_emision = f.id_fuente_emision);

DELETE FROM fuente_emision f
WHERE NOT EXISTS (SELECT 1 FROM catalogo_definitivo c WHERE c.nombre = f.nombre)
  AND NOT EXISTS (SELECT 1 FROM detalle_registro d WHERE d.id_fuente_emision = f.id_fuente_emision)
  AND NOT EXISTS (SELECT 1 FROM carga_traslado_medio x WHERE x.id_fuente_emision = f.id_fuente_emision);

/* Resumen para revisar en la salida de la ejecución */
SELECT a.nombre AS alcance, t.nombre AS tipo, t.modo_carga,
       COUNT(*) FILTER (WHERE f.fecha_baja IS NULL) AS activas,
       COUNT(*) FILTER (WHERE f.fecha_baja IS NOT NULL) AS de_baja
FROM tipo_emision t
JOIN alcance a USING (id_alcance)
LEFT JOIN fuente_emision f USING (id_tipo_emision)
GROUP BY a.nombre, t.nombre, t.modo_carga
ORDER BY 1, 2;

COMMIT;
